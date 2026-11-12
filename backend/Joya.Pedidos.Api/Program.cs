using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi configurada.");

var app = builder.Build();

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

app.Run();

public partial class Program { }
