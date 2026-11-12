namespace Joya.Pedidos.Api.Entities;

public sealed class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int EstoqueDisponivel { get; set; }
    public ICollection<ItemPedido> ItensPedido { get; set; } = new List<ItemPedido>();
}
