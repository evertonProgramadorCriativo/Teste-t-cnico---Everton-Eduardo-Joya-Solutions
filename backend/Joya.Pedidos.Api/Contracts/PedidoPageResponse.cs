namespace Joya.Pedidos.Api.Contracts;

/// <summary>Representa uma página de pedidos e seus metadados.</summary>
public sealed record PedidoPageResponse(
    IReadOnlyList<PedidoListItemResponse> Items,
    int Pagina,
    int TamanhoPagina,
    int TotalRegistros,
    int TotalPaginas);
