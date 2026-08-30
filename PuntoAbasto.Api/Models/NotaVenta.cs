namespace PuntoAbasto.Api.Models;

public class NotaVenta
{
    public Guid Id { get; set; }

    /// <summary>Correlativo NV-0001, lo genera un trigger si viene vacío.</summary>
    public string Numero { get; set; } = string.Empty;

    public Guid PedidoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    /// <summary>emitida | anulada</summary>
    public string Estado { get; set; } = "emitida";

    public string? Observaciones { get; set; }
    public DateTimeOffset FechaEmision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Pedido? Pedido { get; set; }
    public Usuario? Usuario { get; set; }
}
