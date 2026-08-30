namespace PuntoAbasto.Api.Models;

/// <summary>
/// datos_anteriores/datos_nuevos son columnas jsonb: se mapean como
/// string (JSON crudo) y se serializan/deserializan a mano donde haga
/// falta, en vez de forzar un tipo C# fijo para un payload de forma
/// variable según la tabla auditada.
/// </summary>
public class Auditoria
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Tabla { get; set; } = string.Empty;

    /// <summary>INSERT | UPDATE | DELETE</summary>
    public string Operacion { get; set; } = string.Empty;

    public Guid? RegistroId { get; set; }
    public string? DatosAnteriores { get; set; }
    public string? DatosNuevos { get; set; }
    public string? Ip { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Usuario? Usuario { get; set; }
}
