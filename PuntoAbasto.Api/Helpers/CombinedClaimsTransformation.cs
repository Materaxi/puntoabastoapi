using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// ASP.NET Core solo resuelve UNA instancia de IClaimsTransformation por
/// request (vía DI de un solo parámetro, no agrega todas las registradas) —
/// registrar SupabaseRoleClaimsTransformation y PortalClienteClaimsTransformation
/// como dos AddSingleton&lt;IClaimsTransformation, ...&gt; separados hace que solo
/// corra la última registrada, dejando "pa_rol" sin setear nunca para el staff.
/// Esta clase combina ambas en la única instancia que se registra.
/// </summary>
public class CombinedClaimsTransformation : IClaimsTransformation
{
    private readonly SupabaseRoleClaimsTransformation _rol = new();
    private readonly PortalClienteClaimsTransformation _clientePortal = new();

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        principal = await _rol.TransformAsync(principal);
        principal = await _clientePortal.TransformAsync(principal);
        return principal;
    }
}
