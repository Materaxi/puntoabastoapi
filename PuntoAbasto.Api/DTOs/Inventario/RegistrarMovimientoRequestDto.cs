using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Inventario;

/// <summary>
/// Movimiento manual sobre el stock. "salida" no es un valor válido acá:
/// esa la genera únicamente el trigger al confirmar un pedido.
///
/// Para "ajuste", Cantidad es un delta con signo (positivo si el conteo
/// físico encontró más de lo esperado, negativo por merma/producto en mal
/// estado) — no es "el nuevo stock total". Para "entrada" siempre debe
/// ser positiva (una compra).
/// </summary>
public class RegistrarMovimientoRequestDto
{
    [Required]
    public string Tipo { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    [Required, StringLength(255)]
    public string Motivo { get; set; } = string.Empty;
}
