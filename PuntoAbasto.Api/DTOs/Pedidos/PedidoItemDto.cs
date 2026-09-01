namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoItemDto(
    Guid Id,
    Guid? ProductoUnidadId,
    string ProductoNombre,
    string UnidadLabel,
    decimal PrecioUnit,
    decimal Cantidad,
    decimal Subtotal,
    IReadOnlyList<PedidoItemPrecioHistorialDto> HistorialPrecios);
