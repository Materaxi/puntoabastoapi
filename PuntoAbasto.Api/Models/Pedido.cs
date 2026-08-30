using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

public class Pedido
{
    public Guid Id { get; set; }

    /// <summary>Correlativo PED-0001, lo genera un trigger si viene vacío.</summary>
    public string Numero { get; set; } = string.Empty;

    public Guid ClienteId { get; set; }
    public Guid? UsuarioId { get; set; }

    /// <summary>recibido | confirmado | preparando | en_camino | entregado | cancelado</summary>
    public string Estado { get; set; } = "recibido";

    /// <summary>whatsapp | web | telefono</summary>
    public string Origen { get; set; } = "whatsapp";

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string? Notas { get; set; }
    public DateTimeOffset FechaPedido { get; set; }
    public DateTimeOffset? FechaEntregaEst { get; set; }
    public DateTimeOffset? FechaEntregaReal { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Cliente? Cliente { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();

    [JsonIgnore]
    public ICollection<PedidoEstado> HistorialEstados { get; set; } = new List<PedidoEstado>();

    [JsonIgnore]
    public ICollection<InventarioMovimiento> InventarioMovimientos { get; set; } = new List<InventarioMovimiento>();

    [JsonIgnore]
    public NotaVenta? NotaVenta { get; set; }
}
