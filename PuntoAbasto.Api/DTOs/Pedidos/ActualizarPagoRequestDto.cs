namespace PuntoAbasto.Api.DTOs.Pedidos;

public class ActualizarPagoRequestDto
{
    public bool Pagado { get; set; }

    /// <summary>qr | efectivo | transferencia. Requerido cuando Pagado es true; se ignora cuando es false.</summary>
    public string? MetodoPago { get; set; }
}
