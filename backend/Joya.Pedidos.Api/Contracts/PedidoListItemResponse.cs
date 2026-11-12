namespace Joya.Pedidos.Api.Contracts;

/// <summary>Representa um pedido incluído na listagem.</summary>
public sealed record PedidoListItemResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTimeOffset DataPedido,
    int QuantidadeItens,
    decimal ValorTotal);
