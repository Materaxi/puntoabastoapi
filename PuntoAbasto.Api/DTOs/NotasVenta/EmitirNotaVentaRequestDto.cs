using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.NotasVenta;

public class EmitirNotaVentaRequestDto
{
    [Required]
    public Guid PedidoId { get; set; }

    public string? Observaciones { get; set; }
}
