using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>
/// Cliente identificado por teléfono. Los clientes no tienen cuenta ni
/// pasan por Supabase Auth: se crean/reutilizan al recibir un pedido.
/// </summary>
public class Cliente
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? ReferenciaDireccion { get; set; }
    public string? Email { get; set; }
    public int TotalPedidos { get; set; }
    public decimal TotalGastado { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonIgnore]
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
