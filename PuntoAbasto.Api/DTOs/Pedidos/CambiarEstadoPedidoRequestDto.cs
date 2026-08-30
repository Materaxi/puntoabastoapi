using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

public class CambiarEstadoPedidoRequestDto
{
    [Required]
    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }
}
