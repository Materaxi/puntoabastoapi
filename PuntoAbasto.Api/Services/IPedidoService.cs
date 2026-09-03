using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;

namespace PuntoAbasto.Api.Services;

public interface IPedidoService
{
    Task<PedidoDetalleDto> CrearAsync(CrearPedidoRequestDto request, CancellationToken ct);

    Task<PedidoDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResultDto<PedidoListItemDto>> BuscarAsync(
        string? estado, Guid? clienteId, bool? pagado, DateTimeOffset? desde, DateTimeOffset? hasta,
        int page, int pageSize, CancellationToken ct);

    Task<PedidoDetalleDto> CambiarEstadoAsync(
        Guid id, string nuevoEstado, string? observacion, ClaimsPrincipal usuario, CancellationToken ct);

    /// <summary>Marca/desmarca el pago, independiente del estado de entrega.
    /// metodoPago es requerido cuando pagado es true (qr | efectivo | transferencia)
    /// y se ignora cuando es false. Registra quién lo hizo en PEDIDO_PAGO_HISTORIAL.</summary>
    Task<PedidoDetalleDto> ActualizarPagoAsync(
        Guid id, bool pagado, string? metodoPago, ClaimsPrincipal usuario, CancellationToken ct);

    /// <summary>Marca/desmarca el pedido como facturado y recalcula Total (suma 16% de
    /// IVA sobre Subtotal - Descuento cuando facturado es true, ver Helpers.PedidoTotales).
    /// Bloqueado si el pedido está cancelado o ya pagado, mismo criterio que
    /// ActualizarPrecioItemAsync.</summary>
    Task<PedidoDetalleDto> ActualizarFacturadoAsync(Guid id, bool facturado, CancellationToken ct);

    /// <summary>Corrige el precio de un ítem (cliente con precio diferenciado) y
    /// recalcula subtotal/total. Si el pedido ya está entregado, ajusta también
    /// el total_gastado del cliente por la diferencia. Registra quién hizo el
    /// cambio en PEDIDO_ITEM_PRECIO_HISTORIAL.</summary>
    Task<PedidoDetalleDto> ActualizarPrecioItemAsync(
        Guid pedidoId, Guid itemId, decimal nuevoPrecio, ClaimsPrincipal usuario, CancellationToken ct);
}
