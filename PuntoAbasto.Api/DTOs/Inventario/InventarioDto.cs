namespace PuntoAbasto.Api.DTOs.Inventario;

public record InventarioDto(
    Guid Id,
    Guid ProductoUnidadId,
    Guid ProductoId,
    string ProductoNombre,
    string UnidadLabel,
    decimal StockActual,
    decimal StockMinimo,
    decimal? StockMaximo,
    string UnidadMedida,
    bool AlertaActiva,
    DateTimeOffset UpdatedAt);
