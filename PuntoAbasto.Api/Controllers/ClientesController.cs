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

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
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
}
