using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.DTOs.Reportes;

namespace PuntoAbasto.Api.Services;

public interface IReporteService
{
    Task<VentasReporteDto> ObtenerVentasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);

    Task<IReadOnlyList<ProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(
        DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct);

    Task<IReadOnlyList<ClienteTopDto>> ObtenerClientesTopAsync(DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct);

    Task<IReadOnlyList<PedidosPorEstadoDto>> ObtenerPedidosPorEstadoAsync(CancellationToken ct);

    Task<MovimientosResumenDto> ObtenerMovimientosInventarioAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct);

    Task<PagedResultDto<MovimientoHistorialDto>> ObtenerHistorialMovimientosAsync(
        DateOnly? desde, DateOnly? hasta, string? tipo, string? q, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<PedidoListItemDto>> ObtenerPedidosSinNotaVentaAsync(CancellationToken ct);

    /// <summary>Lista de compra unificada: suma las cantidades de cada producto a través
    /// de todos los pedidos en recibido/confirmado/preparando (los que todavía hay que
    /// comprar), sin importar la fecha del pedido.</summary>
    Task<ReporteComprasDto> ObtenerReporteComprasAsync(CancellationToken ct);
}
