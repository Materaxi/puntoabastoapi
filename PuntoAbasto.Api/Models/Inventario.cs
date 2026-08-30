using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

public class Inventario
{
    public Guid Id { get; set; }
    public Guid ProductoUnidadId { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal? StockMaximo { get; set; }

    /// <summary>kg | unidad | amarro | caja | arroba, según el producto</summary>
    public string UnidadMedida { get; set; } = string.Empty;

    public bool AlertaActiva { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ProductoUnidad? ProductoUnidad { get; set; }

    [JsonIgnore]
    public ICollection<InventarioMovimiento> Movimientos { get; set; } = new List<InventarioMovimiento>();
}
