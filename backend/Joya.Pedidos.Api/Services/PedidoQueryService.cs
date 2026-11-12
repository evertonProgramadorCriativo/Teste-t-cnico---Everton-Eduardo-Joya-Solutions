using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Api.Services;

public sealed class PedidoQueryService(PedidosDbContext dbContext) : IPedidoQueryService
{
    public async Task<PedidoListItemResponse?> GetPedidoByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var pedido = await dbContext.Pedidos
            .AsNoTracking()
            .Where(pedido => pedido.Id == id)
            .Select(pedido => new
            {
                pedido.Id,
                pedido.ClienteId,
                ClienteNome = pedido.Cliente.Nome,
                pedido.DataPedido,
                QuantidadeItens = pedido.ItensPedido.Sum(item => (int?)item.Quantidade) ?? 0,
                pedido.ValorTotal
            })
            .SingleOrDefaultAsync(cancellationToken);

        return pedido is null
            ? null
            : new PedidoListItemResponse(
                pedido.Id,
                pedido.ClienteId,
                pedido.ClienteNome,
                new DateTimeOffset(DateTime.SpecifyKind(pedido.DataPedido, DateTimeKind.Utc)),
                pedido.QuantidadeItens,
                pedido.ValorTotal);
    }

    public async Task<PedidoPageResponse> GetPedidosAsync(
        int? clienteId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Pedidos.AsNoTracking();

        if (clienteId.HasValue)
        {
            query = query.Where(pedido => pedido.ClienteId == clienteId.Value);
        }

        if (dataInicio.HasValue)
        {
            var inicio = dataInicio.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(pedido => pedido.DataPedido >= inicio);
        }

        if (dataFim.HasValue)
        {
            var fimExclusivo = dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(pedido => pedido.DataPedido < fimExclusivo);
        }

        var totalRegistros = await query.CountAsync(cancellationToken);
        var offset = (int)((long)(pagina - 1) * tamanhoPagina);
        var pedidos = await query
            .OrderByDescending(pedido => pedido.DataPedido)
            .ThenByDescending(pedido => pedido.Id)
            .Skip(offset)
            .Take(tamanhoPagina)
            .Select(pedido => new
            {
                pedido.Id,
                pedido.ClienteId,
                ClienteNome = pedido.Cliente.Nome,
                pedido.DataPedido,
                QuantidadeItens = pedido.ItensPedido.Sum(item => (int?)item.Quantidade) ?? 0,
                pedido.ValorTotal
            })
            .ToListAsync(cancellationToken);

        var items = pedidos
            .Select(pedido => new PedidoListItemResponse(
                pedido.Id,
                pedido.ClienteId,
                pedido.ClienteNome,
                new DateTimeOffset(DateTime.SpecifyKind(pedido.DataPedido, DateTimeKind.Utc)),
                pedido.QuantidadeItens,
                pedido.ValorTotal))
            .ToArray();

        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanhoPagina);
        return new PedidoPageResponse(items, pagina, tamanhoPagina, totalRegistros, totalPaginas);
    }
}
