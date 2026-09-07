using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Compras;

public class RegistrarCompraRequestDto
{
    [Required]
    public Guid ProductoUnidadId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    /// <summary>Precio de venta a aplicar al producto (sugerido: costoUnitario * 1.30,
    /// editable en el form). Se aplica directo a ProductoUnidad.Precio al registrar
    /// la compra — mismo criterio que ya usa el form de edición de producto, sin
    /// historial de cambios de precio.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a 0.")]
    public decimal PrecioVenta { get; set; }

    [StringLength(150)]
    public string? Proveedor { get; set; }

    public string? Notas { get; set; }
}
