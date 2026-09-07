using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

public class ClientePortalContext : IClientePortalContext
{
    private readonly AppDbContext _db;

    public ClientePortalContext(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Cliente> ObtenerClienteActualAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var authUserId = principal.GetUsuarioId();

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.AuthUserId == authUserId && c.AccesoPortal, ct);

        return cliente ?? throw new UnauthorizedAccessException(
            "El token es válido pero no corresponde a ningún cliente con acceso al portal habilitado.");
    }
}
