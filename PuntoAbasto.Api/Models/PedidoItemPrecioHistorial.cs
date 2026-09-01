using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>Historial de correcciones de precio_unit en PEDIDO_ITEMS (clientes con precio diferenciado).</summary>
public class PedidoItemPrecioHistorial
{
    public Guid Id { get; set; }
    public Guid PedidoItemId { get; set; }
    public Guid? UsuarioId { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [JsonIgnore]
    public PedidoItem? PedidoItem { get; set; }

    public Usuario? Usuario { get; set; }
}
