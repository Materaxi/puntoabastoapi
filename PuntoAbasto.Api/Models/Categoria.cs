using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Emoji { get; set; }
    public int Orden { get; set; }

    [JsonIgnore]
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
