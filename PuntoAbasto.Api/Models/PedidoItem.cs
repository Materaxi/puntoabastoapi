using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>
/// Snapshot inmutable de lo que el cliente compró: nombre, unidad y
/// precio quedan grabados acá aunque el producto cambie de nombre,
/// precio, o incluso se elimine del catálogo más adelante.
/// </summary>
public class PedidoItem
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid? ProductoUnidadId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string UnidadLabel { get; set; } = string.Empty;
    public decimal PrecioUnit { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Subtotal { get; set; }

    [JsonIgnore]
    public Pedido? Pedido { get; set; }

    public ProductoUnidad? ProductoUnidad { get; set; }

    [JsonIgnore]
    public ICollection<PedidoItemPrecioHistorial> HistorialPrecios { get; set; } = new List<PedidoItemPrecioHistorial>();
}
