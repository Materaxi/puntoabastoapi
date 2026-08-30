using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.DTOs.Supabase;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

public class AuthService : IAuthService
{
    public const string SupabaseAuthHttpClientName = "SupabaseAuth";

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<UsuarioDto> ObtenerUsuarioActualAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = principal.GetUsuarioId();

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException(
                $"El JWT es válido pero no existe un usuario '{userId}' en la tabla usuarios " +
                "(¿el trigger on_auth_user_created no corrió, o el usuario fue borrado?).");

        usuario.UltimoLogin = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToDto(usuario);
    }

    public async Task<RefreshTokenResponseDto> RefrescarTokenAsync(string refreshToken, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(SupabaseAuthHttpClientName);

        using var response = await client.PostAsJsonAsync(
            "token?grant_type=refresh_token",
            new { refresh_token = refreshToken },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Auth rechazó el refresh token ({Status}): {Error}", response.StatusCode, error);
            throw new UnauthorizedAccessException(error ?? "No se pudo refrescar la sesión.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Supabase Auth devolvió una respuesta vacía al refrescar el token.");

        return new RefreshTokenResponseDto(payload.AccessToken, payload.RefreshToken, payload.TokenType, payload.ExpiresIn);
    }

    public async Task CerrarSesionAsync(ClaimsPrincipal principal, string accessToken, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(SupabaseAuthHttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Auth respondió {Status} al cerrar sesión: {Error}", response.StatusCode, error);
        }

        var userId = principal.GetUsuarioId();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (usuario is not null)
        {
            usuario.UltimoLogin = null;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static async Task<string?> SafeReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>(cancellationToken: ct);
            return error?.Message;
        }
        catch
        {
            return null;
        }
    }

    private static UsuarioDto MapToDto(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.Email,
        usuario.Rol,
        usuario.Activo,
        usuario.UltimoLogin);
}
