namespace PuntoAbasto.Api.DTOs.Reportes;

/// <summary>Costeo de utilidades en el rango: mismo criterio que VentasReporteDto
/// (pedidos entregado con FechaEntregaReal en el rango) más lo gastado en COMPRAS
/// (por CreatedAt, no tiene "entrega") en el mismo rango.</summary>
public record CosteoReporteDto(
    DateOnly Desde,
    DateOnly Hasta,
    int CantidadPedidos,
    decimal TotalVentas,
    decimal TotalPagado,
    decimal TotalNoPagado,
    decimal TotalFacturado,
    decimal TotalSinFactura,
    int CantidadCompras,
    decimal TotalGastoCompras,
    decimal UtilidadBruta);
