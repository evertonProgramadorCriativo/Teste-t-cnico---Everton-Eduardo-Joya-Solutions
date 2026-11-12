using Joya.Pedidos.Api.Contracts;

namespace Joya.Pedidos.Api.Services;

public enum PedidoCreationError
{
    InvalidRequest,
    ClienteNotFound,
    ProdutoNotFound,
    InsufficientStock,
    TotalExceedsLimit
}

public sealed record PedidoCreationResult(
    PedidoCriadoResponse? Pedido,
    PedidoCreationError? Error,
    string? Detail);
