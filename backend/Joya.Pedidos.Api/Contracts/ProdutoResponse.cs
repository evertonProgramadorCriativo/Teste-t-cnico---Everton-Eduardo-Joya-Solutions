namespace Joya.Pedidos.Api.Contracts;

/// <summary>Representa um produto e seu preço e estoque atuais.</summary>
public sealed record ProdutoResponse(int Id, string Nome, decimal PrecoUnitario, int EstoqueDisponivel);
