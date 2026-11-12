namespace Joya.Pedidos.Api.Contracts;

/// <summary>Confirmação da criação de um pedido.</summary>
public sealed record PedidoCriadoResponse(int Id, DateTimeOffset DataPedido, decimal ValorTotal);
