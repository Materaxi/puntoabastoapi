using System.Security.Claims;
using PuntoAbasto.Api.DTOs;

namespace PuntoAbasto.Api.Services;

public interface IAuthService
{
    /// <summary>Lee el usuario interno correspondiente al JWT validado y actualiza ultimo_login.</summary>
    Task<UsuarioDto> ObtenerUsuarioActualAsync(ClaimsPrincipal principal, CancellationToken ct);

    /// <summary>Proxy hacia Supabase Auth: intercambia un refresh token por un access token nuevo.</summary>
    Task<RefreshTokenResponseDto> RefrescarTokenAsync(string refreshToken, CancellationToken ct);

    /// <summary>Invalida la sesión en Supabase Auth y limpia ultimo_login.</summary>
    Task CerrarSesionAsync(ClaimsPrincipal principal, string accessToken, CancellationToken ct);
}
