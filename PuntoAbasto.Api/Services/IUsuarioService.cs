using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Usuarios;

namespace PuntoAbasto.Api.Services;

public interface IUsuarioService
{
    Task<PagedResultDto<UsuarioDto>> BuscarAsync(bool? activo, string? busqueda, int page, int pageSize, CancellationToken ct);
    Task<UsuarioDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Crea el usuario en Supabase Auth (Admin API); el trigger de schema.sql
    /// crea la fila espejo en public.usuarios automáticamente.</summary>
    Task<UsuarioDto> CrearAsync(CrearUsuarioRequestDto request, CancellationToken ct);

    /// <summary>Actualiza nombre/rol vía Supabase Admin API (para que el trigger los
    /// mantenga sincronizados) y activo/ban_duration para bloquear o restaurar el login.</summary>
    Task<UsuarioDto> ActualizarAsync(Guid id, ActualizarUsuarioRequestDto request, CancellationToken ct);

    /// <summary>Fuerza una contraseña nueva sobre la cuenta de un usuario interno
    /// (recuperación de acceso: se olvidó la suya, otro admin genera una temporal).</summary>
    Task RestablecerPasswordAsync(Guid id, string passwordTemporal, CancellationToken ct);
}
