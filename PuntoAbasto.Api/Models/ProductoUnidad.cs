using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>
/// Cada opción de venta de un producto (Kg, Caja, Amarro, ...), con su
/// propio precio. El inventario y los items de pedido apuntan acá, no
/// a PRODUCTOS, porque el stock depende de la unidad vendida.
/// </summary>
public class ProductoUnidad
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool EsDefault { get; set; }
    public bool Disponible { get; set; } = true;
    public int Orden { get; set; }

    public Producto? Producto { get; set; }
    public Inventario? Inventario { get; set; }

    [JsonIgnore]
    public ICollection<PedidoItem> PedidoItems { get; set; } = new List<PedidoItem>();

    [JsonIgnore]
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
