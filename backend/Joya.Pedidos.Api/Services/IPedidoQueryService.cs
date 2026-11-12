using Joya.Pedidos.Api.Contracts;

namespace Joya.Pedidos.Api.Services;

public interface IPedidoQueryService
{
    Task<PedidoListItemResponse?> GetPedidoByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PedidoPageResponse> GetPedidosAsync(
        int? clienteId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);
}
