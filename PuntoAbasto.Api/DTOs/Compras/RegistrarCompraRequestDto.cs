using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Compras;

public class RegistrarCompraRequestDto
{
    [Required]
    public Guid ProductoUnidadId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    [StringLength(150)]
    public string? Proveedor { get; set; }

    public string? Notas { get; set; }
}
