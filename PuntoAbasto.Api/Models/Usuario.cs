using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.Models;

/// <summary>
/// Espejo de auth.users (Supabase Auth) para el personal interno.
/// El id es el mismo uuid que auth.users.id; la fila se crea/actualiza
/// automáticamente vía trigger (ver schema.sql), nunca desde la API.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>admin | vendedor | delivery</summary>
    public string Rol { get; set; } = "vendedor";

    public bool Activo { get; set; } = true;
    public DateTimeOffset? UltimoLogin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonIgnore]
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    [JsonIgnore]
    public ICollection<PedidoEstado> PedidoEstados { get; set; } = new List<PedidoEstado>();

    [JsonIgnore]
    public ICollection<InventarioMovimiento> InventarioMovimientos { get; set; } = new List<InventarioMovimiento>();

    [JsonIgnore]
    public ICollection<NotaVenta> NotasVenta { get; set; } = new List<NotaVenta>();

    [JsonIgnore]
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}
