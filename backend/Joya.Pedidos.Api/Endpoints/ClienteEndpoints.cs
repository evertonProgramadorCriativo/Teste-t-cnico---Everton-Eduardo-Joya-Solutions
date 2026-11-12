using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Joya.Pedidos.Api.Endpoints;

public static class ClienteEndpoints
{
    public static WebApplication MapClients(this WebApplication app)
    {
        app.MapGet("/clientes", async Task<Ok<IReadOnlyList<ClienteResponse>>> (
            ILookupService lookupService,
            CancellationToken cancellationToken) =>
        {
            var clientes = await lookupService.GetClientesAsync(cancellationToken);
            return TypedResults.Ok(clientes);
        })
        .WithName("GetClientes")
        .WithSummary("Lista os clientes disponíveis")
        .WithDescription("Retorna os clientes carregados no banco para seleção na criação de pedidos.")
        .Produces<IReadOnlyList<ClienteResponse>>(StatusCodes.Status200OK);

        return app;
    }
}
