namespace PuntoAbasto.Api.DTOs.Reportes;

/// <summary>Basado en PEDIDOS.estado = 'entregado' (venta consumada), no en NOTAS_VENTA:
/// emitir la nota es un paso administrativo aparte y no todo pedido entregado la tiene
/// todavía (ver /api/reportes/pedidos-sin-nota-venta).</summary>
public record VentasReporteDto(
    DateOnly Desde,
    DateOnly Hasta,
    int TotalPedidos,
    decimal TotalVentas,
    decimal TotalDescuentos,
    decimal TicketPromedio,
    IReadOnlyList<VentaDiariaDto> SerieDiaria);
