using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

public class CrearPedidoItemDto
{
    [Required]
    public Guid ProductoUnidadId { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal Cantidad { get; set; }
}
