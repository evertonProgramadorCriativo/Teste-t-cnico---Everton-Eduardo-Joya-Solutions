using Joya.Pedidos.Api.Data;
using Joya.Pedidos.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace Joya.Pedidos.Tests;

[CollectionDefinition("SQL Server API", DisableParallelization = true)]
public sealed class SqlServerApiCollection : ICollectionFixture<SqlServerApiFixture>
{
    public const string Name = "SQL Server API";
}

public sealed class SqlServerApiFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__DefaultConnection";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithPassword("Integration!Tests2026")
        .Build();
    private WebApplicationFactory<Program> _factory = null!;
    private string? _previousConnectionString;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "JoyaPedidosIntegration"
        }.ConnectionString;

        _previousConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, connectionString);
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task<SqlServerSeed> ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.ItensPedido; DELETE FROM dbo.Pedidos; DELETE FROM dbo.Produtos; DELETE FROM dbo.Clientes;");

        var clientes = Enumerable.Range(1, 6)
            .Select(index => new Cliente
            {
                Nome = $"Cliente {index}",
                Email = $"cliente{index}@integration.local"
            })
            .ToArray();
        var produtoA = new Produto
        {
            Nome = "Produto A",
            PrecoUnitario = 10.25m,
            EstoqueDisponivel = 1000
        };
        var produtoB = new Produto
        {
            Nome = "Produto B",
            PrecoUnitario = 3.50m,
            EstoqueDisponivel = 3
        };

        dbContext.Clientes.AddRange(clientes);
        dbContext.Produtos.AddRange(produtoA, produtoB);
        await dbContext.SaveChangesAsync();

        return new SqlServerSeed(
            clientes.Select(cliente => cliente.Id).ToArray(),
            produtoA.Id,
            produtoB.Id);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            _previousConnectionString);
        await _sqlServer.DisposeAsync();
    }
}

public sealed record SqlServerSeed(int[] ClienteIds, int ProdutoAId, int ProdutoBId);
