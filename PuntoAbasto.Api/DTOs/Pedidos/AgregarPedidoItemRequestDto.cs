using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

/// <summary>Agrega un producto a un pedido ya creado (el cliente pide sumar
/// algo después de armado el pedido). Si el pedido ya tiene un ítem con la
/// misma unidad, se suma la cantidad al existente en vez de duplicar la fila.</summary>
public class AgregarPedidoItemRequestDto
{
    [Required]
    public Guid ProductoUnidadId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Cantidad { get; set; }
}
