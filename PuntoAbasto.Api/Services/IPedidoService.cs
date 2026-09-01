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
    /// Registra quién lo hizo en PEDIDO_PAGO_HISTORIAL.</summary>
    Task<PedidoDetalleDto> ActualizarPagoAsync(Guid id, bool pagado, ClaimsPrincipal usuario, CancellationToken ct);

    /// <summary>Corrige el precio de un ítem (cliente con precio diferenciado) y
    /// recalcula subtotal/total. Si el pedido ya está entregado, ajusta también
    /// el total_gastado del cliente por la diferencia. Registra quién hizo el
    /// cambio en PEDIDO_ITEM_PRECIO_HISTORIAL.</summary>
    Task<PedidoDetalleDto> ActualizarPrecioItemAsync(
        Guid pedidoId, Guid itemId, decimal nuevoPrecio, ClaimsPrincipal usuario, CancellationToken ct);
}
