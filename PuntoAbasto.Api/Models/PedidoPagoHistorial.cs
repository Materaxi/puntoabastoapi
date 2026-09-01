using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>Historial de quién marcó/desmarcó PEDIDOS.pagado.</summary>
public class PedidoPagoHistorial
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public bool Pagado { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [JsonIgnore]
    public Pedido? Pedido { get; set; }

    public Usuario? Usuario { get; set; }
}
