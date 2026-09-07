namespace PuntoAbasto.Api.Services;

/// <summary>Alta/baja del acceso al portal para un cliente-empresa (toggle en
/// ClienteFormModal del admin). Separado de IUsuarioService porque ese es
/// staff-only (roles admin/vendedor/delivery/almacenero, invite flow atado
/// a esos roles).</summary>
public interface IClientePortalService
{
    /// <summary>Crea la cuenta de Supabase Auth para el cliente y la vincula
    /// (el trigger de Postgres handle_new_user hace el vínculo real, ver
    /// schema.sql). Falla si el cliente ya tiene una cuenta vinculada.</summary>
    Task HabilitarAccesoAsync(Guid clienteId, string email, string passwordTemporal, CancellationToken ct);

    /// <summary>Revoca el acceso (acceso_portal = false) sin desvincular ni
    /// borrar la cuenta de Supabase Auth — alcanza para este alcance, no
    /// hace falta banear la cuenta de verdad.</summary>
    Task RevocarAccesoAsync(Guid clienteId, CancellationToken ct);
}
