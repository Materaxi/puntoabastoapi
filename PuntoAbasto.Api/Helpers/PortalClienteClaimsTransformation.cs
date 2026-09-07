using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// Análogo a <see cref="SupabaseRoleClaimsTransformation"/>: extrae
/// user_metadata.es_cliente_portal del JWT y lo expone como claim propio
/// para que la policy "ClientePortal" pueda usarlo con RequireClaim.
///
/// A propósito NO expone acá el cliente_id del JWT como fuente de verdad de
/// autorización — user_metadata queda "congelado" en el JWT desde el login y
/// podría quedar desactualizado si el vínculo cambia. El cliente_id real se
/// resuelve por request contra la tabla clientes (ver IClientePortalContext).
/// </summary>
public class PortalClienteClaimsTransformation : IClaimsTransformation
{
    public const string EsClientePortalClaimType = "pa_es_cliente_portal";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        if (identity.HasClaim(c => c.Type == EsClientePortalClaimType))
            return Task.FromResult(principal);

        var userMetadataClaim = identity.FindFirst("user_metadata");
        if (userMetadataClaim is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(userMetadataClaim.Value);
                if (doc.RootElement.TryGetProperty("es_cliente_portal", out var esClientePortalElement))
                {
                    var esClientePortal = esClientePortalElement.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.String => bool.TryParse(esClientePortalElement.GetString(), out var parsed) && parsed,
                        _ => false
                    };

                    if (esClientePortal)
                    {
                        identity.AddClaim(new Claim(EsClientePortalClaimType, "true"));
                    }
                }
            }
            catch (JsonException)
            {
                // user_metadata no vino como JSON válido: se ignora, el JWT
                // simplemente no calificará para la policy ClientePortal.
            }
        }

        return Task.FromResult(principal);
    }
}
