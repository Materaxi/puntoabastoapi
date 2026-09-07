using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>
/// Cliente identificado por teléfono. La mayoría no tiene cuenta ni pasa por
/// Supabase Auth (se crean/reutilizan al recibir un pedido) — la excepción es
/// el grupo selecto de clientes-empresa con acceso al portal (AuthUserId).
/// </summary>
public class Cliente
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? ReferenciaDireccion { get; set; }

    /// <summary>Link de Google Maps o coordenadas "lat,long" que el negocio pide por
    /// WhatsApp cuando la dirección de texto no alcanza para ubicar la entrega.</summary>
    public string? UbicacionGps { get; set; }

    public string? Email { get; set; }
    public int TotalPedidos { get; set; }
    public decimal TotalGastado { get; set; }

    /// <summary>Vínculo opcional con auth.users, seteado por el trigger de Postgres
    /// cuando se habilita el acceso al portal (ver ClientePortalService).</summary>
    public Guid? AuthUserId { get; set; }

    /// <summary>Si puede loguearse al portal. Independiente de AuthUserId para poder
    /// revocar sin desvincular ni borrar la cuenta de Supabase Auth.</summary>
    public bool AccesoPortal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonIgnore]
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
