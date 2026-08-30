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

    Task<IReadOnlyList<PedidoListItemDto>> ObtenerPedidosSinNotaVentaAsync(CancellationToken ct);
}
