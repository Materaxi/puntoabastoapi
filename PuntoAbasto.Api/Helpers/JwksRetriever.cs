using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// Supabase expone un JWKS (RFC 7517) plano en /auth/v1/.well-known/jwks.json
/// pero no un documento de descubrimiento OIDC completo, así que no podemos
/// usar OpenIdConnectConfigurationRetriever. Este retriever lee el JWKS
/// directamente y lo envuelve en un ConfigurationManager con auto-refresh,
/// para soportar la rotación de llaves de firma de Supabase.
/// </summary>
public class JwksRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public async Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);
        return new JsonWebKeySet(json);
    }
}
