using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Endpoints;

public static class RelatorioEndpoints
{
    public static WebApplication MapReports(this WebApplication app)
    {
        app.MapGet("/relatorios/top-clientes", async Task<Results<
            Ok<IReadOnlyList<TopClienteResponse>>,
            BadRequest<ProblemDetails>>> (
            IRelatorioService relatorioService,
            CancellationToken cancellationToken,
            DateOnly? dataInicio = null,
            DateOnly? dataFim = null) =>
        {
            if (!dataInicio.HasValue || !dataFim.HasValue)
            {
                return Invalid("Os parâmetros dataInicio e dataFim são obrigatórios.");
            }

            if (dataInicio.Value > dataFim.Value)
            {
                return Invalid("A dataInicio não pode ser posterior à dataFim.");
            }

            if (dataFim == DateOnly.MaxValue)
            {
                return Invalid("A dataFim está fora do intervalo permitido.");
            }

            var inicio = dataInicio.Value.ToDateTime(TimeOnly.MinValue);
            var fimExclusivo = dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var clientes = await relatorioService.ObterTopClientesAsync(
                inicio,
                fimExclusivo,
                cancellationToken);

            return TypedResults.Ok(clientes);
        })
        .WithName("GetTopClientes")
        .WithSummary("Lista os cinco clientes com maior valor comprado")
        .WithDescription("Filtra pelo período inclusivo sem aplicar funções sobre DataPedido.")
        .Produces<IReadOnlyList<TopClienteResponse>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static BadRequest<ProblemDetails> Invalid(string detail)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Período inválido",
            Detail = detail
        });
    }
}
