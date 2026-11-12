using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Api.Services;

public sealed class LookupService(PedidosDbContext dbContext) : ILookupService
{
    public async Task<IReadOnlyList<ClienteResponse>> GetClientesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Clientes
            .AsNoTracking()
            .OrderBy(cliente => cliente.Id)
            .Select(cliente => new ClienteResponse(cliente.Id, cliente.Nome))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProdutoResponse>> GetProdutosAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Produtos
            .AsNoTracking()
            .OrderBy(produto => produto.Id)
            .Select(produto => new ProdutoResponse(
                produto.Id,
                produto.Nome,
                produto.PrecoUnitario,
                produto.EstoqueDisponivel))
            .ToListAsync(cancellationToken);
    }
}
