using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.NotasVenta;

public class AnularNotaVentaRequestDto
{
    [Required, StringLength(500, MinimumLength = 3, ErrorMessage = "Contá el motivo de la anulación.")]
    public string Observaciones { get; set; } = string.Empty;
}
