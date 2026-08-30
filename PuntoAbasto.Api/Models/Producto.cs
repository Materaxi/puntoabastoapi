using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

public class Producto
{
    public Guid Id { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Emoji { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Badge { get; set; }
    public bool Activo { get; set; } = true;
    public bool Disponible { get; set; } = true;
    public int Orden { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Categoria? Categoria { get; set; }

    [JsonIgnore]
    public ICollection<ProductoUnidad> Unidades { get; set; } = new List<ProductoUnidad>();
}
