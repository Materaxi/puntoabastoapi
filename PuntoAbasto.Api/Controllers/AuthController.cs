using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>
/// La API .NET no emite JWT propios: la autenticación la maneja Supabase
/// Auth. Este controller expone /me (leer el usuario interno detrás del
/// JWT ya validado) y hace de proxy para refresh/logout contra Supabase.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Me(CancellationToken ct)
    {
        var usuario = await _authService.ObtenerUsuarioActualAsync(User, ct);
        return Ok(usuario);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponseDto>> Refresh(
        [FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var result = await _authService.RefrescarTokenAsync(request.RefreshToken, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var accessToken = Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        await _authService.CerrarSesionAsync(User, accessToken, ct);
        return NoContent();
    }
}
