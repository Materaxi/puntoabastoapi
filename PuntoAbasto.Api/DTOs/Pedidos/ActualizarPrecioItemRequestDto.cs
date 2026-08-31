using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

/// <summary>Para clientes con precio diferenciado: corrige el precio unitario
/// de un ítem ya cargado. Recalcula subtotal/total del pedido.</summary>
public class ActualizarPrecioItemRequestDto
{
    [Range(0, double.MaxValue)]
    public decimal PrecioUnit { get; set; }
}
