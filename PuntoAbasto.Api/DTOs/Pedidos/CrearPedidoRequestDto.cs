using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Pedidos;

/// <summary>
/// Lo llena el carrito web (o el vendedor, al registrar un pedido recibido
/// por WhatsApp/teléfono). El cliente no tiene cuenta: se identifica y se
/// crea/reutiliza por teléfono.
/// </summary>
public class CrearPedidoRequestDto
{
    [Required, StringLength(20, MinimumLength = 6)]
    public string ClienteTelefono { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ClienteNombre { get; set; } = string.Empty;

    [Required]
    public string ClienteDireccion { get; set; } = string.Empty;

    public string? ClienteReferenciaDireccion { get; set; }

    [EmailAddress]
    public string? ClienteEmail { get; set; }

    /// <summary>whatsapp | web | telefono. Default: "web" (viene del carrito).</summary>
    public string Origen { get; set; } = "web";

    public string? Notas { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Descuento { get; set; }

    [Required, MinLength(1, ErrorMessage = "El pedido debe tener al menos un item.")]
    public List<CrearPedidoItemDto> Items { get; set; } = [];
}
