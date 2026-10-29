using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Catalogo;

public class ActualizarProductoRequestDto
{
    [Required]
    public int CategoriaId { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Codigo { get; set; }

    public string? Descripcion { get; set; }
    public string? Emoji { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Badge { get; set; }
    public bool Activo { get; set; }
    public bool Disponible { get; set; }
    public int Orden { get; set; }
}
