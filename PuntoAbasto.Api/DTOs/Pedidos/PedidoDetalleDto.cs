namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoDetalleDto(
    Guid Id,
    string Numero,
    ClienteResumenDto Cliente,
    string? UsuarioNombre,
    string Estado,
    string Origen,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    string? Notas,
    DateTimeOffset FechaPedido,
    DateTimeOffset? FechaEntregaEst,
    DateTimeOffset? FechaEntregaReal,
    IReadOnlyList<PedidoItemDto> Items,
    IReadOnlyList<PedidoEstadoHistorialDto> HistorialEstados);
