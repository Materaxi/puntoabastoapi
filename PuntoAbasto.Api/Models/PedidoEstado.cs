using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>Historial de transiciones de PEDIDOS.estado. Se registra vía trigger.</summary>
public class PedidoEstado
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [JsonIgnore]
    public Pedido? Pedido { get; set; }

    public Usuario? Usuario { get; set; }
}
