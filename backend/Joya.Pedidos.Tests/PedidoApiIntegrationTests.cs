using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Joya.Pedidos.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Joya.Pedidos.Tests;

[Collection(SqlServerApiCollection.Name)]
public sealed class PedidoApiIntegrationTests
{
    private readonly SqlServerApiFixture _fixture;

    public PedidoApiIntegrationTests(SqlServerApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostPedido_Valido_Retorna201EGravaPedidoEEstoque()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            seed.ClienteIds[0],
            new CriarPedidoItemRequest(seed.ProdutoAId, 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var pedidoCriado = Assert.IsType<PedidoCriadoResponse>(
            await response.Content.ReadFromJsonAsync<PedidoCriadoResponse>());
        Assert.Equal(20.50m, pedidoCriado.ValorTotal);
        Assert.Equal($"/pedidos/{pedidoCriado.Id}", response.Headers.Location?.ToString());

        await using var scope = _fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        Assert.Equal(998, await dbContext.Produtos
            .Where(produto => produto.Id == seed.ProdutoAId)
            .Select(produto => produto.EstoqueDisponivel)
            .SingleAsync());
        Assert.Equal(1, await dbContext.Pedidos.CountAsync());
        Assert.Equal(1, await dbContext.ItensPedido.CountAsync());
    }

    [Fact]
    public async Task PostPedido_ClienteInexistente_Retorna404()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            int.MaxValue,
            new CriarPedidoItemRequest(seed.ProdutoAId, 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostPedido_SemItens_Retorna400()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await _fixture.Client.PostAsJsonAsync(
            "/pedidos",
            new CriarPedidoRequest { ClienteId = seed.ClienteIds[0], Itens = [] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPedido_QuantidadeZero_Retorna400()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            seed.ClienteIds[0],
            new CriarPedidoItemRequest(seed.ProdutoAId, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPedido_ProdutoInexistente_Retorna404()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            seed.ClienteIds[0],
            new CriarPedidoItemRequest(int.MaxValue, 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostPedido_EstoqueInsuficiente_Retorna409()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            seed.ClienteIds[0],
            new CriarPedidoItemRequest(seed.ProdutoBId, 4));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostPedido_FalhaEmUmItem_ReverteEstoqueDosDemais()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        using var response = await PostPedidoAsync(
            seed.ClienteIds[0],
            new CriarPedidoItemRequest(seed.ProdutoAId, 2),
            new CriarPedidoItemRequest(seed.ProdutoBId, 4));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var scope = _fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        Assert.Equal(1000, await EstoqueAsync(dbContext, seed.ProdutoAId));
        Assert.Equal(3, await EstoqueAsync(dbContext, seed.ProdutoBId));
        Assert.Empty(await dbContext.Pedidos.ToListAsync());
        Assert.Empty(await dbContext.ItensPedido.ToListAsync());
    }

    [Fact]
    public async Task GetPedidos_AplicaFiltroPorClientePeriodoEPaginacao()
    {
        var seed = await _fixture.ResetDatabaseAsync();
        int pedidoMaisAntigoId;

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
            var pedidos = new[]
            {
                CriarPedido(seed.ClienteIds[0], seed.ProdutoAId, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 2),
                CriarPedido(seed.ClienteIds[0], seed.ProdutoAId, new DateTime(2026, 10, 11, 23, 59, 59, DateTimeKind.Utc), 3),
                CriarPedido(seed.ClienteIds[1], seed.ProdutoAId, new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 4),
                CriarPedido(seed.ClienteIds[0], seed.ProdutoAId, new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc), 5)
            };
            dbContext.Pedidos.AddRange(pedidos);
            await dbContext.SaveChangesAsync();
            pedidoMaisAntigoId = pedidos[0].Id;
        }

        var comFiltro = await _fixture.Client.GetFromJsonAsync<PedidoPageResponse>(
            $"/pedidos?clienteId={seed.ClienteIds[0]}&dataInicio=2026-10-10&dataFim=2026-10-11&pagina=1&tamanhoPagina=10");
        Assert.NotNull(comFiltro);
        Assert.Equal(2, comFiltro.TotalRegistros);
        Assert.All(comFiltro.Items, pedido => Assert.Equal(seed.ClienteIds[0], pedido.ClienteId));

        var segundaPagina = await _fixture.Client.GetFromJsonAsync<PedidoPageResponse>(
            "/pedidos?dataInicio=2026-10-10&dataFim=2026-10-11&pagina=2&tamanhoPagina=2");
        Assert.NotNull(segundaPagina);
        Assert.Equal(3, segundaPagina.TotalRegistros);
        Assert.Equal(2, segundaPagina.TotalPaginas);
        Assert.Single(segundaPagina.Items);
        Assert.Equal(pedidoMaisAntigoId, segundaPagina.Items[0].Id);
    }

    [Fact]
    public async Task GetTopClientes_OrdenaLimitaCincoERespeitaLimitesInclusivos()
    {
        var seed = await _fixture.ResetDatabaseAsync();

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
            dbContext.Pedidos.AddRange(
                CriarPedido(seed.ClienteIds[0], seed.ProdutoAId, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), 60),
                CriarPedido(seed.ClienteIds[1], seed.ProdutoAId, new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 50),
                CriarPedido(seed.ClienteIds[2], seed.ProdutoAId, new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 40),
                CriarPedido(seed.ClienteIds[3], seed.ProdutoAId, new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 30),
                CriarPedido(seed.ClienteIds[4], seed.ProdutoAId, new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc), 20),
                CriarPedido(seed.ClienteIds[5], seed.ProdutoAId, new DateTime(2026, 10, 12, 23, 59, 59, DateTimeKind.Utc), 25),
                CriarPedido(seed.ClienteIds[5], seed.ProdutoAId, new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc), 100));
            await dbContext.SaveChangesAsync();
        }

        var ranking = await _fixture.Client.GetFromJsonAsync<TopClienteResponse[]>(
            "/relatorios/top-clientes?dataInicio=2026-10-10&dataFim=2026-10-12");

        Assert.NotNull(ranking);
        Assert.Equal(5, ranking.Length);
        Assert.Equal(
            new[] { seed.ClienteIds[0], seed.ClienteIds[1], seed.ClienteIds[2], seed.ClienteIds[3], seed.ClienteIds[5] },
            ranking.Select(cliente => cliente.ClienteId));
        Assert.Equal(
            new[] { 615.00m, 512.50m, 410.00m, 307.50m, 256.25m },
            ranking.Select(cliente => cliente.TotalComprado));
        Assert.Equal(1, ranking[^1].QuantidadePedidos);
    }

    private Task<HttpResponseMessage> PostPedidoAsync(
        int clienteId,
        params CriarPedidoItemRequest[] itens) =>
        _fixture.Client.PostAsJsonAsync(
            "/pedidos",
            new CriarPedidoRequest { ClienteId = clienteId, Itens = itens });

    private static Pedido CriarPedido(
        int clienteId,
        int produtoId,
        DateTime dataPedido,
        int quantidade) =>
        new()
        {
            ClienteId = clienteId,
            DataPedido = dataPedido,
            ValorTotal = quantidade * 10.25m,
            ItensPedido =
            [
                new ItemPedido
                {
                    ProdutoId = produtoId,
                    Quantidade = quantidade,
                    PrecoUnitario = 10.25m
                }
            ]
        };

    private static Task<int> EstoqueAsync(PedidosDbContext dbContext, int produtoId) =>
        dbContext.Produtos
            .Where(produto => produto.Id == produtoId)
            .Select(produto => produto.EstoqueDisponivel)
            .SingleAsync();
}
