using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.DTOs.Reportes;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>
/// Los reportes financieros (ventas, ranking de productos/clientes) son solo
/// para "Admin" — son datos del dueño del negocio. Los operativos (estado de
/// pedidos, movimientos de inventario) también los puede ver un vendedor.
/// </summary>
[ApiController]
[Route("api/reportes")]
[Produces("application/json")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>Resumen de ventas + serie diaria en un rango. Default: últimos 30 días.</summary>
    [HttpGet("ventas")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(VentasReporteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentasReporteDto>> Ventas(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var reporte = await _reporteService.ObtenerVentasAsync(desde, hasta, ct);
        return Ok(reporte);
    }

    [HttpGet("productos-mas-vendidos")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoMasVendidoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoMasVendidoDto>>> ProductosMasVendidos(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var reporte = await _reporteService.ObtenerProductosMasVendidosAsync(desde, hasta, top, ct);
        return Ok(reporte);
    }

    [HttpGet("clientes-top")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteTopDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteTopDto>>> ClientesTop(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var reporte = await _reporteService.ObtenerClientesTopAsync(desde, hasta, top, ct);
        return Ok(reporte);
    }

    /// <summary>Foto operativa actual: cuántos pedidos hay en cada estado ahora mismo.</summary>
    [HttpGet("pedidos-por-estado")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(IReadOnlyList<PedidosPorEstadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PedidosPorEstadoDto>>> PedidosPorEstado(CancellationToken ct)
    {
        var reporte = await _reporteService.ObtenerPedidosPorEstadoAsync(ct);
        return Ok(reporte);
    }

    /// <summary>Entradas/salidas/ajustes en el rango, separando mermas reales de reversiones por cancelación.</summary>
    [HttpGet("inventario-movimientos")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(MovimientosResumenDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MovimientosResumenDto>> MovimientosInventario(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var reporte = await _reporteService.ObtenerMovimientosInventarioAsync(desde, hasta, ct);
        return Ok(reporte);
    }

    /// <summary>Historial detallado de movimientos (quién, qué, cuánto), paginado y filtrable — para auditoría.</summary>
    [HttpGet("inventario-movimientos-historial")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(PagedResultDto<MovimientoHistorialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<MovimientoHistorialDto>>> HistorialMovimientosInventario(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] string? tipo, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var reporte = await _reporteService.ObtenerHistorialMovimientosAsync(desde, hasta, tipo, q, page, pageSize, ct);
        return Ok(reporte);
    }

    /// <summary>Pedidos entregados a los que todavía no se les emitió la nota de venta (gap administrativo).</summary>
    [HttpGet("pedidos-sin-nota-venta")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(IReadOnlyList<PedidoListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PedidoListItemDto>>> PedidosSinNotaVenta(CancellationToken ct)
    {
        var reporte = await _reporteService.ObtenerPedidosSinNotaVentaAsync(ct);
        return Ok(reporte);
    }

    /// <summary>Lista de compra: suma las cantidades de cada producto a través de todos los
    /// pedidos en recibido/confirmado/preparando, para ir a comprar lo que falta.</summary>
    [HttpGet("compras")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ReporteComprasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteComprasDto>> ReporteCompras(CancellationToken ct)
    {
        var reporte = await _reporteService.ObtenerReporteComprasAsync(ct);
        return Ok(reporte);
    }
}
