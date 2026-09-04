namespace PuntoAbasto.Api.DTOs.Reportes;

/// <summary>CantidadAComprar ya descuenta StockActual (nunca negativo): si el
/// almacén ya cubre lo que piden los pedidos pendientes, da 0.</summary>
public record ItemCompraDto(
    string ProductoNombre,
    string UnidadLabel,
    decimal CantidadNecesaria,
    decimal StockActual,
    decimal CantidadAComprar);
