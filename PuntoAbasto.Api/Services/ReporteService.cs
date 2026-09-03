using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.DTOs.Reportes;
using PuntoAbasto.Api.Helpers;

namespace PuntoAbasto.Api.Services;

public class ReporteService : IReporteService
{
    private const int RangoMaximoDias = 366;
    private const int DefaultDiasHaciaAtras = 29;

    private readonly AppDbContext _db;

    public ReporteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VentasReporteDto> ObtenerVentasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, desdeFecha, hastaFecha) = ResolverRango(desde, hasta);

        var pedidos = await _db.Pedidos
            .Where(p => p.Estado == "entregado" && p.FechaEntregaReal >= desdeUtc && p.FechaEntregaReal <= hastaUtc)
            .Select(p => new { p.FechaEntregaReal, p.Total, p.Descuento })
            .ToListAsync(ct);

        var totalPedidos = pedidos.Count;
        var totalVentas = pedidos.Sum(p => p.Total);
        var totalDescuentos = pedidos.Sum(p => p.Descuento);
        var ticketPromedio = totalPedidos == 0 ? 0 : totalVentas / totalPedidos;

        // Se agrupa en memoria (no en SQL): agrupar por fecha-sin-hora a partir de un
        // timestamptz no traduce bien a Npgsql, y para el volumen de un negocio
        // familiar esto no pesa nada.
        var serieDiaria = pedidos
            .GroupBy(p => DateOnly.FromDateTime(p.FechaEntregaReal!.Value.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new VentaDiariaDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .ToList();

        return new VentasReporteDto(desdeFecha, hastaFecha, totalPedidos, totalVentas, totalDescuentos, ticketPromedio, serieDiaria);
    }

    public async Task<IReadOnlyList<ProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(
        DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, _, _) = ResolverRango(desde, hasta);
        top = NormalizarTop(top);

        // Dos ajustes sobre la traducción de EF Core acá, encontrados corriendo
        // esto contra Postgres real (compilaba bien, tiraba InvalidOperationException
        // recién en runtime):
        //  1. Filtrar PedidoItems por p.Pedido!.Estado (navegación) y agrupar en el
        //     mismo query no se puede traducir a SQL. Se resuelve primero el set de
        //     pedido_id válidos y se filtra por esa columna escalar.
        //  2. Proyectar un GroupBy+aggregates directo a un record vía constructor
        //     y encadenar OrderByDescending tampoco se traduce. Se materializa un
        //     tipo anónimo con ToListAsync y el orden/Take/DTO final se arma en memoria.
        var pedidoIdsQuery = _db.Pedidos
            .Where(p => p.Estado == "entregado" && p.FechaEntregaReal >= desdeUtc && p.FechaEntregaReal <= hastaUtc)
            .Select(p => p.Id);

        var agregados = await _db.PedidoItems
            .Where(i => pedidoIdsQuery.Contains(i.PedidoId))
            .GroupBy(i => new { i.ProductoNombre, i.UnidadLabel })
            .Select(g => new
            {
                g.Key.ProductoNombre,
                g.Key.UnidadLabel,
                CantidadVendida = g.Sum(i => i.Cantidad),
                TotalIngresos = g.Sum(i => i.Subtotal)
            })
            .ToListAsync(ct);

        return agregados
            .OrderByDescending(a => a.TotalIngresos)
            .Take(top)
            .Select(a => new ProductoMasVendidoDto(a.ProductoNombre, a.UnidadLabel, a.CantidadVendida, a.TotalIngresos))
            .ToList();
    }

    public async Task<IReadOnlyList<ClienteTopDto>> ObtenerClientesTopAsync(
        DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, _, _) = ResolverRango(desde, hasta);
        top = NormalizarTop(top);

        var agregados = await _db.Pedidos
            .Where(p => p.Estado == "entregado" && p.FechaEntregaReal >= desdeUtc && p.FechaEntregaReal <= hastaUtc)
            .GroupBy(p => new { p.ClienteId, p.Cliente!.Nombre, p.Cliente.Telefono })
            .Select(g => new
            {
                g.Key.ClienteId,
                g.Key.Nombre,
                g.Key.Telefono,
                CantidadPedidos = g.Count(),
                TotalGastado = g.Sum(p => p.Total)
            })
            .ToListAsync(ct);

        return agregados
            .OrderByDescending(a => a.TotalGastado)
            .Take(top)
            .Select(a => new ClienteTopDto(a.ClienteId, a.Nombre, a.Telefono, a.CantidadPedidos, a.TotalGastado))
            .ToList();
    }

    public async Task<IReadOnlyList<PedidosPorEstadoDto>> ObtenerPedidosPorEstadoAsync(CancellationToken ct)
    {
        var conteos = await _db.Pedidos
            .GroupBy(p => p.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);

        var porEstado = conteos.ToDictionary(c => c.Estado, c => c.Cantidad);

        // Se completan los estados sin pedidos con 0, para que el dashboard
        // siempre muestre las 6 columnas del flujo aunque alguna esté vacía.
        return PedidoEstadoTransiciones.EstadosValidos
            .Select(estado => new PedidosPorEstadoDto(estado, porEstado.GetValueOrDefault(estado)))
            .ToList();
    }

    public async Task<MovimientosResumenDto> ObtenerMovimientosInventarioAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, desdeFecha, hastaFecha) = ResolverRango(desde, hasta);

        var movimientos = await _db.InventarioMovimientos
            .Where(m => m.CreatedAt >= desdeUtc && m.CreatedAt <= hastaUtc)
            .Select(m => new { m.Tipo, m.Cantidad, m.PedidoId })
            .ToListAsync(ct);

        var totalEntradas = movimientos.Where(m => m.Tipo == "entrada").Sum(m => m.Cantidad);
        var totalSalidas = movimientos.Where(m => m.Tipo == "salida").Sum(m => m.Cantidad);

        // "ajuste" mezcla dos cosas muy distintas: correcciones manuales (merma,
        // conteo físico — pedido_id nulo) y las reversiones automáticas que hace
        // fn_revertir_stock al cancelar un pedido confirmado (pedido_id NOT NULL,
        // ver schema.sql). Separamos ambas para no contar una reversión como merma.
        var totalAjustesManuales = movimientos.Where(m => m.Tipo == "ajuste" && m.PedidoId == null).Sum(m => m.Cantidad);
        var totalReversiones = movimientos.Where(m => m.Tipo == "ajuste" && m.PedidoId != null).Sum(m => m.Cantidad);

        var mermasAgregadas = await _db.InventarioMovimientos
            .Where(m => m.Tipo == "ajuste" && m.PedidoId == null && m.Cantidad < 0 &&
                        m.CreatedAt >= desdeUtc && m.CreatedAt <= hastaUtc)
            .GroupBy(m => new { m.Inventario!.ProductoUnidad!.Producto!.Nombre, m.Inventario.ProductoUnidad.Label })
            .Select(g => new { g.Key.Nombre, g.Key.Label, TotalMerma = -g.Sum(m => m.Cantidad) })
            .ToListAsync(ct);

        var topMermas = mermasAgregadas
            .OrderByDescending(m => m.TotalMerma)
            .Take(10)
            .Select(m => new MermaProductoDto(m.Nombre, m.Label, m.TotalMerma))
            .ToList();

        return new MovimientosResumenDto(desdeFecha, hastaFecha, totalEntradas, totalSalidas, totalAjustesManuales, totalReversiones, topMermas);
    }

    /// <summary>Historial detallado de movimientos (quién, qué, cuánto), paginado — para auditoría.</summary>
    public async Task<PagedResultDto<MovimientoHistorialDto>> ObtenerHistorialMovimientosAsync(
        DateOnly? desde, DateOnly? hasta, string? tipo, string? q, int page, int pageSize, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, _, _) = ResolverRango(desde, hasta);
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.InventarioMovimientos
            .Where(m => m.CreatedAt >= desdeUtc && m.CreatedAt <= hastaUtc);

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoNormalizado = tipo.Trim().ToLowerInvariant();
            query = query.Where(m => m.Tipo == tipoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(m => EF.Functions.ILike(m.Inventario!.ProductoUnidad!.Producto!.Nombre, patron));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MovimientoHistorialDto(
                m.Id,
                m.Inventario!.ProductoUnidad!.Producto!.Nombre,
                m.Inventario.ProductoUnidad.Label,
                m.Tipo,
                m.Cantidad,
                m.StockAnterior,
                m.StockNuevo,
                m.Motivo,
                m.Usuario != null ? m.Usuario.Nombre : null,
                m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResultDto<MovimientoHistorialDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<PedidoListItemDto>> ObtenerPedidosSinNotaVentaAsync(CancellationToken ct)
    {
        return await _db.Pedidos
            .Include(p => p.Cliente)
            .Where(p => p.Estado == "entregado" && p.NotaVenta == null)
            .OrderByDescending(p => p.FechaEntregaReal)
            .Select(p => new PedidoListItemDto(p.Id, p.Numero, p.Cliente!.Nombre, p.Cliente.Telefono, p.Estado, p.Origen, p.Total, p.Pagado, p.MetodoPago, p.Facturado, p.FechaPedido))
            .ToListAsync(ct);
    }

    public async Task<ReporteComprasDto> ObtenerReporteComprasAsync(CancellationToken ct)
    {
        // Sin filtro de fecha a propósito: puede haber pedidos de días anteriores
        // que todavía no se despacharon y también hay que comprarles.
        var estadosPendientes = new[] { "recibido", "confirmado", "preparando" };

        var pedidosQuery = _db.Pedidos.Where(p => estadosPendientes.Contains(p.Estado));

        var pedidos = await pedidosQuery
            .Include(p => p.Cliente)
            .OrderBy(p => p.FechaPedido)
            .Select(p => new PedidoListItemDto(
                p.Id, p.Numero, p.Cliente!.Nombre, p.Cliente.Telefono, p.Estado, p.Origen,
                p.Total, p.Pagado, p.MetodoPago, p.Facturado, p.FechaPedido))
            .ToListAsync(ct);

        // Mismo patrón que ObtenerProductosMasVendidosAsync: resolver primero los
        // pedido_id válidos como columna escalar, filtrar PedidoItems por eso.
        var pedidoIdsQuery = pedidosQuery.Select(p => p.Id);

        var agregados = await _db.PedidoItems
            .Where(i => pedidoIdsQuery.Contains(i.PedidoId))
            .GroupBy(i => new { i.ProductoNombre, i.UnidadLabel })
            .Select(g => new
            {
                g.Key.ProductoNombre,
                g.Key.UnidadLabel,
                CantidadTotal = g.Sum(i => i.Cantidad)
            })
            .ToListAsync(ct);

        var items = agregados
            .OrderBy(a => a.ProductoNombre)
            .Select(a => new ItemCompraDto(a.ProductoNombre, a.UnidadLabel, a.CantidadTotal))
            .ToList();

        return new ReporteComprasDto(items, pedidos);
    }

    public async Task<CosteoReporteDto> ObtenerCosteoAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var (desdeUtc, hastaUtc, desdeFecha, hastaFecha) = ResolverRango(desde, hasta);

        var pedidos = await _db.Pedidos
            .Where(p => p.Estado == "entregado" && p.FechaEntregaReal >= desdeUtc && p.FechaEntregaReal <= hastaUtc)
            .Select(p => new { p.Total, p.Pagado, p.Facturado })
            .ToListAsync(ct);

        var totalVentas = pedidos.Sum(p => p.Total);
        var totalPagado = pedidos.Where(p => p.Pagado).Sum(p => p.Total);
        var totalNoPagado = pedidos.Where(p => !p.Pagado).Sum(p => p.Total);
        var totalFacturado = pedidos.Where(p => p.Facturado).Sum(p => p.Total);
        var totalSinFactura = pedidos.Where(p => !p.Facturado).Sum(p => p.Total);

        var compras = await _db.Compras
            .Where(c => c.CreatedAt >= desdeUtc && c.CreatedAt <= hastaUtc)
            .Select(c => c.CostoTotal)
            .ToListAsync(ct);

        var totalGastoCompras = compras.Sum();

        return new CosteoReporteDto(
            desdeFecha, hastaFecha,
            pedidos.Count, totalVentas, totalPagado, totalNoPagado, totalFacturado, totalSinFactura,
            compras.Count, totalGastoCompras,
            totalVentas - totalGastoCompras);
    }

    private static (DateTimeOffset DesdeUtc, DateTimeOffset HastaUtc, DateOnly DesdeFecha, DateOnly HastaFecha) ResolverRango(
        DateOnly? desde, DateOnly? hasta)
    {
        var hastaFecha = hasta ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var desdeFecha = desde ?? hastaFecha.AddDays(-DefaultDiasHaciaAtras);

        if (desdeFecha > hastaFecha)
        {
            throw new ArgumentException("'desde' no puede ser posterior a 'hasta'.");
        }

        if (hastaFecha.DayNumber - desdeFecha.DayNumber > RangoMaximoDias)
        {
            throw new ArgumentException($"El rango no puede superar {RangoMaximoDias} días.");
        }

        var desdeUtc = new DateTimeOffset(desdeFecha.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var hastaUtc = new DateTimeOffset(hastaFecha.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        return (desdeUtc, hastaUtc, desdeFecha, hastaFecha);
    }

    private static int NormalizarTop(int top) => top is < 1 or > 100 ? 10 : top;
}
