using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Supabase;

namespace PuntoAbasto.Api.Services;

public class ClientePortalService : IClientePortalService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClientePortalService> _logger;

    public ClientePortalService(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<ClientePortalService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task HabilitarAccesoAsync(Guid clienteId, string email, string passwordTemporal, CancellationToken ct)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {clienteId}.");

        if (cliente.AuthUserId is not null)
        {
            throw new ArgumentException("Este cliente ya tiene una cuenta de portal vinculada.");
        }

        if (string.IsNullOrWhiteSpace(passwordTemporal) || passwordTemporal.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        }

        var client = _httpClientFactory.CreateClient(UsuarioService.SupabaseAdminHttpClientName);

        using var response = await client.PostAsJsonAsync("users", new SupabaseAdminCreateUserRequest
        {
            Email = email,
            Password = passwordTemporal,
            EmailConfirm = true,
            UserMetadata = new Dictionary<string, string>
            {
                ["es_cliente_portal"] = "true",
                ["cliente_id"] = clienteId.ToString()
            }
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Admin API rechazó la creación de cuenta de portal para cliente {ClienteId} ({Status}): {Error}",
                clienteId, response.StatusCode, error);
            throw new ArgumentException(error ?? "No se pudo crear la cuenta de portal en Supabase Auth.");
        }

        // El trigger on_auth_user_created (rama es_cliente_portal) corre dentro de la
        // misma transacción del INSERT en auth.users y vincula auth_user_id/acceso_portal
        // acá mismo, así que para cuando la respuesta HTTP vuelve ya quedó hecho — pero
        // lo hizo por fuera de este DbContext (via Postgres), así que la instancia de
        // "cliente" que ya cargamos arriba quedó con los valores viejos en memoria.
        // La recargamos para que quien llame a este método (y reuse el mismo DbContext,
        // como ClientesController) vea el estado real en vez del que quedó cacheado.
        await _db.Entry(cliente).ReloadAsync(ct);
    }

    public async Task RevocarAccesoAsync(Guid clienteId, CancellationToken ct)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {clienteId}.");

        cliente.AccesoPortal = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RestablecerPasswordAsync(Guid clienteId, string passwordTemporal, CancellationToken ct)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {clienteId}.");

        if (cliente.AuthUserId is null)
        {
            throw new ArgumentException("Este cliente todavía no tiene acceso al portal habilitado.");
        }

        if (string.IsNullOrWhiteSpace(passwordTemporal) || passwordTemporal.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        }

        var client = _httpClientFactory.CreateClient(UsuarioService.SupabaseAdminHttpClientName);

        using var response = await client.PutAsJsonAsync($"users/{cliente.AuthUserId}", new SupabaseAdminUpdateUserRequest
        {
            Password = passwordTemporal
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await SafeReadErrorAsync(response, ct);
            _logger.LogWarning("Supabase Admin API rechazó el restablecimiento de contraseña del cliente {ClienteId} ({Status}): {Error}",
                clienteId, response.StatusCode, error);
            throw new ArgumentException(error ?? "No se pudo restablecer la contraseña en Supabase Auth.");
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
}
