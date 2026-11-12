using Joya.Pedidos.Api.Contracts;

namespace Joya.Pedidos.Api.Services;

public interface IPedidoCreationService
{
    Task<PedidoCreationResult> CreateAsync(
        CriarPedidoRequest? request,
        CancellationToken cancellationToken);
}
