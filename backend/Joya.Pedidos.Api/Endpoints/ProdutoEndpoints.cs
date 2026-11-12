using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Joya.Pedidos.Api.Endpoints;

public static class ProdutoEndpoints
{
    public static WebApplication MapProducts(this WebApplication app)
    {
        app.MapGet("/produtos", async Task<Ok<IReadOnlyList<ProdutoResponse>>> (
            ILookupService lookupService,
            CancellationToken cancellationToken) =>
        {
            var produtos = await lookupService.GetProdutosAsync(cancellationToken);
            return TypedResults.Ok(produtos);
        })
        .WithName("GetProdutos")
        .WithSummary("Lista os produtos disponíveis")
        .WithDescription("Retorna os produtos carregados no banco com preço e estoque atuais.")
        .Produces<IReadOnlyList<ProdutoResponse>>(StatusCodes.Status200OK);

        return app;
    }
}
