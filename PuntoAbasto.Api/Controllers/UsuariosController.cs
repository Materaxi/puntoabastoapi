using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Usuarios;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>Gestión de personal interno (admin/vendedor/delivery). Solo Admin: son cuentas
/// con acceso al panel, no un dato operativo del día a día.</summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize(Policy = "Admin")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<UsuarioDto>>> Buscar(
        [FromQuery] bool? activo,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _usuarioService.BuscarAsync(activo, q, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var usuario = await _usuarioService.ObtenerPorIdAsync(id, ct);
        return Ok(usuario);
    }

    /// <summary>Crea el usuario directo en Supabase Auth (ya confirmado, sin email de invitación).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioDto>> Crear([FromBody] CrearUsuarioRequestDto request, CancellationToken ct)
    {
        var usuario = await _usuarioService.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.Id }, usuario);
    }

    /// <summary>Actualiza nombre/rol/activo. Desactivar banea de verdad la sesión en Supabase Auth.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Actualizar(
        Guid id, [FromBody] ActualizarUsuarioRequestDto request, CancellationToken ct)
    {
        var usuario = await _usuarioService.ActualizarAsync(id, request, ct);
        return Ok(usuario);
    }

    /// <summary>Recuperación de acceso: otro admin le genera una temporal nueva a un
    /// usuario interno que se olvidó la suya.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestablecerPassword(
        Guid id, [FromBody] RestablecerPasswordUsuarioRequestDto request, CancellationToken ct)
    {
        await _usuarioService.RestablecerPasswordAsync(id, request.PasswordTemporal, ct);
        return NoContent();
    }
}
