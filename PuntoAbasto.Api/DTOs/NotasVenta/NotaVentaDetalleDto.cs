using PuntoAbasto.Api.DTOs.Pedidos;

namespace PuntoAbasto.Api.DTOs.NotasVenta;

public record NotaVentaDetalleDto(
    Guid Id,
    string Numero,
    Guid PedidoId,
    string PedidoNumero,
    ClienteResumenDto Cliente,
    string? UsuarioNombre,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    string Estado,
    string? Observaciones,
    DateTimeOffset FechaEmision,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PedidoItemDto> Items);
