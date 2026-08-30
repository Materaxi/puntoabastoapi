using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PuntoAbasto.Api.Helpers;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Lee el id del usuario interno (mismo uuid que auth.users) del claim "sub" del JWT.</summary>
    public static Guid GetUsuarioId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException("El token no trae el claim 'sub'.");

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("El claim 'sub' del token no es un uuid válido.");
    }
}
