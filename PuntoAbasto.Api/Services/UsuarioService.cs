using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Supabase;
using PuntoAbasto.Api.DTOs.Usuarios;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

public class UsuarioService : IUsuarioService
{
    public const string SupabaseAdminHttpClientName = "SupabaseAdmin";
    private const int PageSizeMaximo = 100;
    private static readonly string[] RolesValidos = ["admin", "vendedor", "delivery"];

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<UsuarioService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PagedResultDto<UsuarioDto>> BuscarAsync(bool? activo, string? busqueda, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var query = _db.Usuarios.AsQueryable();

        if (activo is not null)
        {
            query = query.Where(u => u.Activo == activo);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.Nombre, $"%{busqueda}%") ||
                EF.Functions.ILike(u.Email, $"%{busqueda}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<UsuarioDto>(items.Select(MapToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<UsuarioDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el usuario {id}.");
        return MapToDto(usuario);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequestDto request, CancellationToken ct)
    {
        var rol = ValidarRol(request.Rol);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        }

        var yaExiste = await _db.Usuarios.AnyAsync(u => u.Email == request.Email, ct);
        if (yaExiste)
        {
            throw new ArgumentException($"Ya existe un usuario con el email {request.Email}.");
        }

        var client = _httpClientFactory.CreateClient(SupabaseAdminHttpClientName);

        using var response = await client.PostAsJsonAsync("users", new SupabaseAdminCreateUserRequest
        {
            Email = request.Email,
            Password = request.Password,
            EmailConfirm = true,
            UserMetadata = new Dictionary<string, string> { ["nombre"] = request.Nombre, ["rol"] = rol }
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Admin API rechazó la creación de usuario ({Status}): {Error}", response.StatusCode, error);
            throw new ArgumentException(error ?? "No se pudo crear el usuario en Supabase Auth.");
        }

        var creado = await response.Content.ReadFromJsonAsync<SupabaseAdminUserResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Supabase Admin API devolvió una respuesta vacía al crear el usuario.");

        // El trigger on_auth_user_created corre dentro de la misma transacción del INSERT
        // en auth.users, así que para cuando la respuesta HTTP vuelve la fila espejo ya existe.
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == creado.Id, ct);

        return usuario is not null
            ? MapToDto(usuario)
            : new UsuarioDto(creado.Id, request.Nombre, creado.Email, rol, true, null);
    }

    public async Task<UsuarioDto> ActualizarAsync(Guid id, ActualizarUsuarioRequestDto request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el usuario {id}.");

        var rol = ValidarRol(request.Rol);

        var client = _httpClientFactory.CreateClient(SupabaseAdminHttpClientName);

        using var response = await client.PutAsJsonAsync($"users/{id}", new SupabaseAdminUpdateUserRequest
        {
            UserMetadata = new Dictionary<string, string> { ["nombre"] = request.Nombre, ["rol"] = rol },
            // Banear ~100 años es la forma estándar de "desactivar" en Supabase Auth: bloquea
            // el login de verdad, a diferencia de solo apagar el flag local usuarios.activo.
            BanDuration = request.Activo ? "none" : "876000h"
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Admin API rechazó la actualización del usuario {Id} ({Status}): {Error}", id, response.StatusCode, error);
            throw new ArgumentException(error ?? "No se pudo actualizar el usuario en Supabase Auth.");
        }

        // nombre/rol los sincroniza el trigger on_auth_user_updated a partir del user_metadata
        // que acabamos de mandar; activo es local (no existe en auth.users), lo escribimos acá.
        await _db.Entry(usuario).ReloadAsync(ct);
        usuario.Activo = request.Activo;
        await _db.SaveChangesAsync(ct);

        return MapToDto(usuario);
    }

    private static string ValidarRol(string rol)
    {
        var normalizado = rol.Trim().ToLowerInvariant();
        if (!RolesValidos.Contains(normalizado))
        {
            throw new ArgumentException($"Rol inválido '{rol}'. Debe ser uno de: {string.Join(", ", RolesValidos)}.");
        }

        return normalizado;
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
        usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.Activo, usuario.UltimoLogin);
}
