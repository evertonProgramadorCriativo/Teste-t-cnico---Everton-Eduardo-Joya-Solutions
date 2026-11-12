using Joya.Pedidos.Api.Contracts;
using Joya.Pedidos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Api.Services;

public sealed class RelatorioService(PedidosDbContext dbContext) : IRelatorioService
{
    public async Task<IReadOnlyList<TopClienteResponse>> ObterTopClientesAsync(
        DateTime dataInicio,
        DateTime dataFimExclusivo,
        CancellationToken cancellationToken)
    {
        return await dbContext.Database
            .SqlQuery<TopClienteResponse>($"""
                SELECT TOP (5)
                    c.Id AS ClienteId,
                    c.Nome AS ClienteNome,
                    SUM(p.ValorTotal) AS TotalComprado,
                    COUNT(p.Id) AS QuantidadePedidos
                FROM dbo.Pedidos AS p
                INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
                WHERE p.DataPedido >= {dataInicio}
                  AND p.DataPedido < {dataFimExclusivo}
                GROUP BY c.Id, c.Nome
                ORDER BY TotalComprado DESC, c.Id ASC
                """)
            .ToListAsync(cancellationToken);
    }
}
