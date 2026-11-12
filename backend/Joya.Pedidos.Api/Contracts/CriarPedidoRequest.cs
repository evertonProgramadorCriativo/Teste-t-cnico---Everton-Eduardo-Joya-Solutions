namespace Joya.Pedidos.Api.Contracts;

/// <summary>Dados enviados para criar um pedido.</summary>
public sealed record CriarPedidoRequest
{
    public int ClienteId { get; init; }
    public IReadOnlyList<CriarPedidoItemRequest?>? Itens { get; init; }
}

/// <summary>Produto e quantidade solicitados para um pedido.</summary>
public sealed record CriarPedidoItemRequest(int ProdutoId, int Quantidade);
