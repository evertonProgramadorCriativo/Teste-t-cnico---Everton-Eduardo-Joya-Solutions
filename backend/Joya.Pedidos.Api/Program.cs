using Joya.Pedidos.Api.Data;
using Joya.Pedidos.Api.Endpoints;
using Joya.Pedidos.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi configurada.");

builder.Services.AddDbContext<PedidosDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IPedidoCreationService, PedidoCreationService>();
builder.Services.AddScoped<IPedidoQueryService, PedidoQueryService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/health", async (CancellationToken cancellationToken, ILogger<Program> logger) =>
{
    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT (SELECT COUNT(*) FROM dbo.Clientes), (SELECT COUNT(*) FROM dbo.Produtos);",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return Results.Ok(new
        {
            status = "ok",
            banco = "conectado",
            clientes = reader.GetInt32(0),
            produtos = reader.GetInt32(1)
        });
    }
    catch (SqlException exception)
    {
        logger.LogWarning(exception, "Não foi possível conectar ao SQL Server.");
        return Results.Problem(
            title: "Banco indisponível",
            detail: "A API não conseguiu conectar ao SQL Server.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapClients();
app.MapProducts();
app.MapOrders();
app.MapReports();

app.Run();

public partial class Program { }
