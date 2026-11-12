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

        return Results.Ok(new { status = "ok", banco = "conectado" });
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
