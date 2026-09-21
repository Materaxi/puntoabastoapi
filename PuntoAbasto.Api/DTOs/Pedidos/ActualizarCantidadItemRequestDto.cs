using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

/// <summary>Corrige la cantidad de un ítem ya cargado en un pedido. Recalcula
/// subtotal/total del pedido y, si el stock ya se descontó, ajusta el
/// inventario por la diferencia.</summary>
public class ActualizarCantidadItemRequestDto
{
    [Range(0.01, double.MaxValue)]
    public decimal Cantidad { get; set; }
}
