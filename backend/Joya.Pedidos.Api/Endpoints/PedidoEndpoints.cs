using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Endpoints;

public static class PedidoEndpoints
{
    public static WebApplication MapOrders(this WebApplication app)
    {
        app.MapGet("/pedidos/{id:int}", async Task<Results<Ok<PedidoListItemResponse>, NotFound<ProblemDetails>>> (
            int id,
            IPedidoQueryService pedidoQueryService,
            CancellationToken cancellationToken) =>
        {
            var pedido = await pedidoQueryService.GetPedidoByIdAsync(id, cancellationToken);
            return pedido is null
                ? TypedResults.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Pedido não encontrado",
                    Detail = "O pedido informado não foi encontrado."
                })
                : TypedResults.Ok(pedido);
        })
        .WithName("GetPedidoById")
        .WithSummary("Consulta um pedido pelo identificador")
        .WithDescription("Retorna os dados do pedido e seus principais totais.")
        .Produces<PedidoListItemResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        app.MapPost("/pedidos", async Task<Results<
            Created<PedidoCriadoResponse>,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            Conflict<ProblemDetails>>> (
            CriarPedidoRequest? request,
            IPedidoCreationService pedidoCreationService,
            CancellationToken cancellationToken) =>
        {
            var result = await pedidoCreationService.CreateAsync(request, cancellationToken);
            if (result.Error is null && result.Pedido is not null)
            {
                return TypedResults.Created($"/pedidos/{result.Pedido.Id}", result.Pedido);
            }

            var problem = new ProblemDetails
            {
                Title = result.Error switch
                {
                    PedidoCreationError.InvalidRequest => "Pedido inválido",
                    PedidoCreationError.ClienteNotFound or PedidoCreationError.ProdutoNotFound => "Recurso não encontrado",
                    PedidoCreationError.InsufficientStock => "Estoque insuficiente",
                    PedidoCreationError.TotalExceedsLimit => "Valor do pedido inválido",
                    _ => "Não foi possível criar o pedido"
                },
                Detail = result.Detail
            };

            return result.Error switch
            {
                PedidoCreationError.InvalidRequest or PedidoCreationError.TotalExceedsLimit =>
                    TypedResults.BadRequest(problem),
                PedidoCreationError.ClienteNotFound or PedidoCreationError.ProdutoNotFound =>
                    TypedResults.NotFound(problem),
                PedidoCreationError.InsufficientStock =>
                    TypedResults.Conflict(problem),
                _ => throw new InvalidOperationException("O serviço retornou um resultado de criação inconsistente.")
            };
        })
        .WithName("CreatePedido")
        .WithSummary("Cria um pedido e baixa o estoque")
        .WithDescription("Calcula os valores com os preços atuais do banco e persiste pedido, itens e estoque em uma transação.")
        .Produces<PedidoCriadoResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        app.MapGet("/pedidos", async Task<Results<Ok<PedidoPageResponse>, BadRequest<ProblemDetails>>> (
            IPedidoQueryService pedidoQueryService,
            CancellationToken cancellationToken,
            int? clienteId = null,
            DateOnly? dataInicio = null,
            DateOnly? dataFim = null,
            int pagina = 1,
            int tamanhoPagina = 10) =>
        {
            if (clienteId is <= 0)
            {
                return Invalid("O parâmetro clienteId deve ser maior que zero.");
            }

            if (dataInicio.HasValue && dataFim.HasValue && dataInicio.Value > dataFim.Value)
            {
                return Invalid("A dataInicio não pode ser posterior à dataFim.");
            }

            if (dataFim == DateOnly.MaxValue)
            {
                return Invalid("A dataFim está fora do intervalo permitido.");
            }

            if (pagina < 1)
            {
                return Invalid("O parâmetro pagina deve ser maior ou igual a 1.");
            }

            if (tamanhoPagina is < 1 or > 50)
            {
                return Invalid("O parâmetro tamanhoPagina deve estar entre 1 e 50.");
            }

            if (pagina > int.MaxValue / tamanhoPagina)
            {
                return Invalid("O número da página excede o intervalo permitido.");
            }

            var pedidos = await pedidoQueryService.GetPedidosAsync(
                clienteId,
                dataInicio,
                dataFim,
                pagina,
                tamanhoPagina,
                cancellationToken);

            return TypedResults.Ok(pedidos);
        })
        .WithName("GetPedidos")
        .WithSummary("Lista pedidos com filtros e paginação")
        .WithDescription("Filtra pedidos por cliente e datas inclusivas, ordenando do mais recente para o mais antigo.")
        .Produces<PedidoPageResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static BadRequest<ProblemDetails> Invalid(string detail)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Parâmetros inválidos",
            Detail = detail
        });
    }
}
