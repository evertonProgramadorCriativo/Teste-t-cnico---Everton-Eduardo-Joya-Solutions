using Joya.Pedidos.Api.Contracts;

namespace Joya.Pedidos.Api.Services;

public interface ILookupService
{
    Task<IReadOnlyList<ClienteResponse>> GetClientesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProdutoResponse>> GetProdutosAsync(CancellationToken cancellationToken);
}
