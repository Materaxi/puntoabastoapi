using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Catalogo;

public class CategoriaRequestDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Slug { get; set; } = string.Empty;

    public string? Emoji { get; set; }

    public int Orden { get; set; }
}
