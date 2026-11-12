using Joya.Pedidos.Api.Contracts;

namespace Joya.Pedidos.Api.Services;

public interface IRelatorioService
{
    Task<IReadOnlyList<TopClienteResponse>> ObterTopClientesAsync(
        DateTime dataInicio,
        DateTime dataFimExclusivo,
        CancellationToken cancellationToken);
}
