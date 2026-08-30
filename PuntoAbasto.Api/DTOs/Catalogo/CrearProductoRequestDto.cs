using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Catalogo;

public class CrearProductoRequestDto
{
    [Required]
    public int CategoriaId { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public string? Emoji { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Badge { get; set; }
    public int Orden { get; set; }

    [Required, MinLength(1, ErrorMessage = "El producto debe tener al menos una unidad de venta.")]
    public List<CrearProductoUnidadRequestDto> Unidades { get; set; } = [];
}
