using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Joya.Pedidos.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Api.Services;

public sealed class PedidoCreationService(PedidosDbContext dbContext) : IPedidoCreationService
{
    private const decimal MaxPedidoTotal = 9999999999999999.99m;

    public async Task<PedidoCreationResult> CreateAsync(
        CriarPedidoRequest? request,
        CancellationToken cancellationToken)
    {
        if (request?.Itens is not { Count: > 0 } itens
            || itens.Any(item => item is null || item.Quantidade <= 0))
        {
            return InvalidRequest("O pedido deve conter itens com quantidades inteiras maiores que zero.");
        }

        var quantidadesPorProduto = new SortedDictionary<int, long>();
        foreach (var item in itens)
        {
            var quantidadeAtual = quantidadesPorProduto.GetValueOrDefault(item!.ProdutoId);
            if (quantidadeAtual + item.Quantidade > int.MaxValue)
            {
                return InvalidRequest("A quantidade total de um produto excede o limite permitido.");
            }

            quantidadesPorProduto[item.ProdutoId] = quantidadeAtual + item.Quantidade;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (!await dbContext.Clientes.AnyAsync(
                cliente => cliente.Id == request.ClienteId,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(PedidoCreationError.ClienteNotFound, "O cliente informado não foi encontrado.");
        }

        var produtos = await dbContext.Produtos
            .AsNoTracking()
            .Where(produto => quantidadesPorProduto.Keys.Contains(produto.Id))
            .Select(produto => new { produto.Id, produto.PrecoUnitario })
            .ToDictionaryAsync(produto => produto.Id, produto => produto.PrecoUnitario, cancellationToken);

        if (produtos.Count != quantidadesPorProduto.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(PedidoCreationError.ProdutoNotFound, "Um ou mais produtos informados não foram encontrados.");
        }

        var valorTotal = 0m;
        foreach (var (produtoId, quantidade) in quantidadesPorProduto)
        {
            valorTotal += produtos[produtoId] * quantidade;
            if (valorTotal > MaxPedidoTotal)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(PedidoCreationError.TotalExceedsLimit, "O valor total excede o limite permitido.");
            }
        }

        foreach (var (produtoId, quantidadeLong) in quantidadesPorProduto)
        {
            var quantidade = (int)quantidadeLong;
            var estoqueAtualizado = await dbContext.Produtos
                .Where(produto => produto.Id == produtoId && produto.EstoqueDisponivel >= quantidade)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        produto => produto.EstoqueDisponivel,
                        produto => produto.EstoqueDisponivel - quantidade),
                    cancellationToken);

            if (estoqueAtualizado != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(
                    PedidoCreationError.InsufficientStock,
                    $"Estoque insuficiente para o produto {produtoId}.");
            }
        }

        var dataPedido = DateTime.UtcNow;
        var pedido = new Pedido
        {
            ClienteId = request.ClienteId,
            DataPedido = dataPedido,
            ValorTotal = valorTotal,
            ItensPedido = quantidadesPorProduto
                .Select(item => new ItemPedido
                {
                    ProdutoId = item.Key,
                    Quantidade = (int)item.Value,
                    PrecoUnitario = produtos[item.Key]
                })
                .ToList()
        };

        dbContext.Pedidos.Add(pedido);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PedidoCreationResult(
            new PedidoCriadoResponse(
                pedido.Id,
                new DateTimeOffset(dataPedido, TimeSpan.Zero),
                valorTotal),
            null,
            null);
    }

    private static PedidoCreationResult InvalidRequest(string detail)
    {
        return Failure(PedidoCreationError.InvalidRequest, detail);
    }

    private static PedidoCreationResult Failure(PedidoCreationError error, string detail)
    {
        return new PedidoCreationResult(null, error, detail);
    }
}
