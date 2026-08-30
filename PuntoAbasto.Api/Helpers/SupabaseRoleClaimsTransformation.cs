using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// El JWT de Supabase trae el rol dentro de "user_metadata" como un objeto
/// JSON anidado, no como un claim plano "user_metadata.rol". Este transformer
/// extrae user_metadata.rol una sola vez y lo expone como un claim propio
/// (<see cref="RoleClaimType"/>) para que las policies de autorización
/// (RequireClaim) puedan usarlo directamente.
/// </summary>
public class SupabaseRoleClaimsTransformation : IClaimsTransformation
{
    public const string RoleClaimType = "pa_rol";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        if (identity.HasClaim(c => c.Type == RoleClaimType))
            return Task.FromResult(principal);

        var userMetadataClaim = identity.FindFirst("user_metadata");
        if (userMetadataClaim is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(userMetadataClaim.Value);
                if (doc.RootElement.TryGetProperty("rol", out var rolElement) &&
                    rolElement.ValueKind == JsonValueKind.String)
                {
                    var rol = rolElement.GetString();
                    if (!string.IsNullOrWhiteSpace(rol))
                    {
                        identity.AddClaim(new Claim(RoleClaimType, rol));
                    }
                }
            }
            catch (JsonException)
            {
                // user_metadata no vino como JSON válido: se ignora, el usuario
                // simplemente no tendrá el claim de rol y las policies lo rechazarán.
            }
        }

        return Task.FromResult(principal);
    }
}
