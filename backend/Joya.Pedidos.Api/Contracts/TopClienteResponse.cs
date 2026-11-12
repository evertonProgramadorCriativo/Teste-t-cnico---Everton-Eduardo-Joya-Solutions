namespace Joya.Pedidos.Api.Contracts;

/// <summary>Cliente classificado pelo valor comprado em um período.</summary>
public sealed record TopClienteResponse(
    int ClienteId,
    string ClienteNome,
    decimal TotalComprado,
    int QuantidadePedidos);
