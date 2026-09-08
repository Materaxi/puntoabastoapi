using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Clientes;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
[Authorize(Policy = "AdminOVendedor")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly IClientePortalService _clientePortalService;

    public ClientesController(IClienteService clienteService, IClientePortalService clientePortalService)
    {
        _clienteService = clienteService;
        _clientePortalService = clientePortalService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ClienteListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ClienteListItemDto>>> Buscar(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _clienteService.BuscarAsync(q, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDetalleDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var cliente = await _clienteService.ObtenerPorIdAsync(id, ct);
        return Ok(cliente);
    }

    /// <summary>Alta manual (ej. cliente que llamó por teléfono y aún no hizo su primer pedido).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClienteDetalleDto>> Crear([FromBody] CrearClienteRequestDto request, CancellationToken ct)
    {
        var cliente = await _clienteService.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> Actualizar(
        Guid id, [FromBody] ActualizarClienteRequestDto request, CancellationToken ct)
    {
        var cliente = await _clienteService.ActualizarAsync(id, request, ct);
        return Ok(cliente);
    }

    /// <summary>Da de alta el acceso al portal B2B para este cliente (grupo selecto de
    /// empresas). La contraseña temporal la genera/copia el usuario en el admin y se la
    /// comunica manualmente (WhatsApp), no hay email transaccional.</summary>
    [HttpPost("{id:guid}/acceso-portal")]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> HabilitarAccesoPortal(
        Guid id, [FromBody] HabilitarAccesoPortalRequestDto request, CancellationToken ct)
    {
        await _clientePortalService.HabilitarAccesoAsync(id, request.Email, request.PasswordTemporal, ct);
        var cliente = await _clienteService.ObtenerPorIdAsync(id, ct);
        return Ok(cliente);
    }

    [HttpDelete("{id:guid}/acceso-portal")]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> RevocarAccesoPortal(Guid id, CancellationToken ct)
    {
        await _clientePortalService.RevocarAccesoAsync(id, ct);
        var cliente = await _clienteService.ObtenerPorIdAsync(id, ct);
        return Ok(cliente);
    }

    /// <summary>Recuperación de acceso: el cliente-empresa se olvidó su contraseña y avisa
    /// por WhatsApp, el admin genera una nueva temporal acá y se la reenvía. El cliente ya
    /// puede elegir su propia contraseña desde el portal una vez que entra con la temporal.</summary>
    [HttpPost("{id:guid}/acceso-portal/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestablecerPasswordPortal(
        Guid id, [FromBody] RestablecerPasswordPortalRequestDto request, CancellationToken ct)
    {
        await _clientePortalService.RestablecerPasswordAsync(id, request.PasswordTemporal, ct);
        return NoContent();
    }
}
