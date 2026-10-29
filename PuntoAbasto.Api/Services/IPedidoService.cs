using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;

namespace PuntoAbasto.Api.Services;

public interface IPedidoService
{
    Task<PedidoDetalleDto> CrearAsync(CrearPedidoRequestDto request, CancellationToken ct);

    /// <summary>Crea un pedido para un cliente-portal ya autenticado (nunca resuelto por
    /// teléfono): origen fijo "web", sin descuento. Ver PortalController.</summary>
    Task<PedidoDetalleDto> CrearParaClienteAsync(Guid clienteId, CrearPedidoPortalRequestDto request, CancellationToken ct);

    Task<PedidoDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>estados: lista explícita de estados a incluir, o null para no filtrar
    /// por estado. Una lista vacía es válida y devuelve 0 resultados (a diferencia de
    /// null) — así un checklist de checkboxes sin nada tildado no trae "todo".
    /// clienteNombre: búsqueda parcial, sin distinguir mayúsculas, para el buscador
    /// de pedidos por nombre de cliente del panel.</summary>
    Task<PagedResultDto<PedidoListItemDto>> BuscarAsync(
        IReadOnlyList<string>? estados, Guid? clienteId, string? clienteNombre, bool? pagado,
        DateTimeOffset? desde, DateTimeOffset? hasta, bool excluirEntregadosPagados,
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

    /// <summary>Agrega un producto a un pedido ya creado (bloqueado si está cancelado
    /// o pagado). Si ya hay un ítem con la misma unidad, suma la cantidad al existente
    /// en vez de duplicar la fila. Si el pedido ya pasó de "recibido" (el stock ya se
    /// descontó al confirmar), ajusta también el inventario.</summary>
    Task<PedidoDetalleDto> AgregarItemAsync(
        Guid pedidoId, Guid productoUnidadId, decimal cantidad, ClaimsPrincipal usuario, CancellationToken ct);

    /// <summary>Corrige la cantidad de un ítem ya cargado (bloqueado si el pedido está
    /// cancelado o pagado). Ajusta el inventario por la diferencia si el stock ya se
    /// había descontado.</summary>
    Task<PedidoDetalleDto> ActualizarCantidadItemAsync(
        Guid pedidoId, Guid itemId, decimal nuevaCantidad, ClaimsPrincipal usuario, CancellationToken ct);

    /// <summary>Elimina un ítem de un pedido ya creado (bloqueado si está cancelado,
    /// pagado, o es el único ítem del pedido). Revierte el inventario si el stock ya
    /// se había descontado.</summary>
    Task<PedidoDetalleDto> EliminarItemAsync(
        Guid pedidoId, Guid itemId, ClaimsPrincipal usuario, CancellationToken ct);
}
