namespace PuntoAbasto.Api.DTOs.Catalogo;

/// <summary>Vista interna de una unidad, con el detalle de inventario para el panel.</summary>
public record ProductoUnidadInternaDto(
    Guid Id,
    string Label,
    decimal Precio,
    bool EsDefault,
    int Orden,
    Guid InventarioId,
    decimal StockActual,
    decimal StockMinimo,
    decimal? StockMaximo,
    string UnidadMedida,
    bool AlertaActiva);
