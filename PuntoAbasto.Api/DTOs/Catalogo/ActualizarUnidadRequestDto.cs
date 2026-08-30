using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Catalogo;

public class ActualizarUnidadRequestDto
{
    [Required, StringLength(50)]
    public string Label { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    public bool EsDefault { get; set; }

    public int Orden { get; set; }
}
