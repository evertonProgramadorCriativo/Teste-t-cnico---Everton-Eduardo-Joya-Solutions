using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Joya.Pedidos.Api.Entities;
using Joya.Pedidos.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Joya.Pedidos.Tests;

public sealed class PedidoCreationServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private PedidosDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PedidosDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new PedidosDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _dbContext.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Cliente de teste",
            Email = "cliente@teste.local"
        });
        _dbContext.Produtos.AddRange(
            new Produto
            {
                Id = 1,
                Nome = "Produto A",
                PrecoUnitario = 10.25m,
                EstoqueDisponivel = 10
            },
            new Produto
            {
                Id = 2,
                Nome = "Produto B",
                PrecoUnitario = 3.50m,
                EstoqueDisponivel = 3
            });
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_RejeitaClienteInexistenteSemAlterarEstoque()
    {
        var result = await Service().CreateAsync(
            new CriarPedidoRequest
            {
                ClienteId = 999,
                Itens = [new CriarPedidoItemRequest(1, 1)]
            },
            CancellationToken.None);

        Assert.Equal(PedidoCreationError.ClienteNotFound, result.Error);
        Assert.Empty(await _dbContext.Pedidos.ToListAsync());
        Assert.Equal(10, await EstoqueAsync(1));
    }

    [Fact]
    public async Task CreateAsync_RejeitaPedidoSemItens()
    {
        var result = await Service().CreateAsync(
            new CriarPedidoRequest { ClienteId = 1, Itens = [] },
            CancellationToken.None);

        Assert.Equal(PedidoCreationError.InvalidRequest, result.Error);
        Assert.Empty(await _dbContext.Pedidos.ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_RejeitaQuantidadeNaoPositiva(int quantidade)
    {
        var result = await Service().CreateAsync(
            Request(new CriarPedidoItemRequest(1, quantidade)),
            CancellationToken.None);

        Assert.Equal(PedidoCreationError.InvalidRequest, result.Error);
        Assert.Empty(await _dbContext.Pedidos.ToListAsync());
        Assert.Equal(10, await EstoqueAsync(1));
    }

    [Fact]
    public async Task CreateAsync_RejeitaProdutoInexistenteSemCriarPedido()
    {
        var result = await Service().CreateAsync(
            Request(new CriarPedidoItemRequest(999, 1)),
            CancellationToken.None);

        Assert.Equal(PedidoCreationError.ProdutoNotFound, result.Error);
        Assert.Empty(await _dbContext.Pedidos.ToListAsync());
        Assert.Equal(10, await EstoqueAsync(1));
    }

    [Fact]
    public async Task CreateAsync_RestauraTodoEstoqueQuandoUmDosProdutosNaoTemSaldo()
    {
        var result = await Service().CreateAsync(
            Request(
                new CriarPedidoItemRequest(1, 2),
                new CriarPedidoItemRequest(2, 4)),
            CancellationToken.None);

        Assert.Equal(PedidoCreationError.InsufficientStock, result.Error);
        Assert.Empty(await _dbContext.Pedidos.ToListAsync());
        Assert.Equal(10, await EstoqueAsync(1));
        Assert.Equal(3, await EstoqueAsync(2));
    }

    [Fact]
    public async Task CreateAsync_CalculaTotalComPrecoDoBanco()
    {
        var result = await Service().CreateAsync(
            Request(
                new CriarPedidoItemRequest(1, 2),
                new CriarPedidoItemRequest(2, 1)),
            CancellationToken.None);

        var pedido = Assert.IsType<PedidoCriadoResponse>(result.Pedido);
        Assert.Null(result.Error);
        Assert.Equal(24.00m, pedido.ValorTotal);

        var item = await _dbContext.ItensPedido
            .SingleAsync(item => item.ProdutoId == 1);
        Assert.Equal(10.25m, item.PrecoUnitario);
    }

    [Fact]
    public async Task CreateAsync_SomaProdutosRepetidosAntesDeValidarEGravar()
    {
        var result = await Service().CreateAsync(
            Request(
                new CriarPedidoItemRequest(1, 2),
                new CriarPedidoItemRequest(1, 3)),
            CancellationToken.None);

        var pedido = Assert.IsType<PedidoCriadoResponse>(result.Pedido);
        Assert.Null(result.Error);
        Assert.Equal(51.25m, pedido.ValorTotal);

        var item = await _dbContext.ItensPedido.SingleAsync();
        Assert.Equal(1, item.ProdutoId);
        Assert.Equal(5, item.Quantidade);
    }

    [Fact]
    public async Task CreateAsync_BaixaEstoqueDeCadaProdutoEGravaPedido()
    {
        var result = await Service().CreateAsync(
            Request(
                new CriarPedidoItemRequest(1, 3),
                new CriarPedidoItemRequest(2, 2)),
            CancellationToken.None);

        Assert.IsType<PedidoCriadoResponse>(result.Pedido);
        Assert.Null(result.Error);
        Assert.Equal(7, await EstoqueAsync(1));
        Assert.Equal(1, await EstoqueAsync(2));
        Assert.Single(await _dbContext.Pedidos.ToListAsync());
        Assert.Equal(2, await _dbContext.ItensPedido.CountAsync());
    }

    private PedidoCreationService Service() => new(_dbContext);

    private static CriarPedidoRequest Request(params CriarPedidoItemRequest[] itens) =>
        new() { ClienteId = 1, Itens = itens };

    private Task<int> EstoqueAsync(int produtoId) =>
        _dbContext.Produtos
            .Where(produto => produto.Id == produtoId)
            .Select(produto => produto.EstoqueDisponivel)
            .SingleAsync();
}
