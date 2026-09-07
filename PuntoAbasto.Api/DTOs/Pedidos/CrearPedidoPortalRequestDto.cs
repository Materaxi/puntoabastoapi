using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

/// <summary>
/// Lo llena un cliente-empresa autenticado en el portal (/api/portal/pedidos).
/// A diferencia de <see cref="CrearPedidoRequestDto"/>, no trae datos de
/// cliente: el cliente ya existe y se resuelve por el JWT, nunca por teléfono.
/// </summary>
public class CrearPedidoPortalRequestDto
{
    public string? Notas { get; set; }

    [Required, MinLength(1, ErrorMessage = "El pedido debe tener al menos un item.")]
    public List<CrearPedidoItemDto> Items { get; set; } = [];
}
