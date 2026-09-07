using System.Security.Claims;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

/// <summary>
/// Resuelve a qué Cliente pertenece el JWT autenticado bajo la policy
/// "ClientePortal". Centralizado acá (en vez de repetir el lookup en cada
/// controller) para que sea el único lugar que decide "este JWT = este
/// cliente_id", siempre consultando el estado actual de la tabla clientes
/// (nunca confiando en un cliente_id embebido en el propio JWT).
/// </summary>
public interface IClientePortalContext
{
    /// <summary>Lanza UnauthorizedAccessException si el JWT es válido pero no
    /// corresponde a ningún cliente con acceso al portal habilitado.</summary>
    Task<Cliente> ObtenerClienteActualAsync(ClaimsPrincipal principal, CancellationToken ct);
}
