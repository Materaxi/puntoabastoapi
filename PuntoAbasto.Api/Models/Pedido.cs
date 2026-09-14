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

    /// <summary>Independiente de Estado: la entrega y el pago son hechos distintos
    /// (se puede entregar y cobrar días después). No participa de PedidoEstadoTransiciones.</summary>
    public bool Pagado { get; set; }
    public DateTimeOffset? FechaPago { get; set; }

    /// <summary>qr | efectivo | transferencia. Null si Pagado es false.</summary>
    public string? MetodoPago { get; set; }

    /// <summary>qr | efectivo | transferencia. Lo elige el cliente al hacer el pedido
    /// (checkout del storefront); es una declaración de intención, no confirma que ya
    /// esté pagado — eso lo sigue marcando el staff en MetodoPago/Pagado.</summary>
    public string? FormaPago { get; set; }

    /// <summary>Si es true, Total incluye el 16% de IVA sobre (Subtotal - Descuento),
    /// ver Helpers.PedidoTotales. Independiente de Estado y Pagado.</summary>
    public bool Facturado { get; set; }

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
    public ICollection<PedidoPagoHistorial> HistorialPago { get; set; } = new List<PedidoPagoHistorial>();

    [JsonIgnore]
    public ICollection<InventarioMovimiento> InventarioMovimientos { get; set; } = new List<InventarioMovimiento>();

    [JsonIgnore]
    public NotaVenta? NotaVenta { get; set; }
}
