using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidosController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    /// <summary>
    /// Crea un pedido. Lo llama el carrito web directamente (sin login: el
    /// cliente se identifica por teléfono) o el panel interno al registrar
    /// un pedido recibido por WhatsApp/teléfono.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PedidoDetalleDto>> Crear([FromBody] CrearPedidoRequestDto request, CancellationToken ct)
    {
        var pedido = await _pedidoService.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = pedido.Id }, pedido);
    }

    /// <summary>Panel interno: lista pedidos con filtros de estado/cliente/fecha.</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<PedidoListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PedidoListItemDto>>> Buscar(
        [FromQuery] string? estado,
        [FromQuery] Guid? clienteId,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _pedidoService.BuscarAsync(estado, clienteId, desde, hasta, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDetalleDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var pedido = await _pedidoService.ObtenerPorIdAsync(id, ct);
        return Ok(pedido);
    }

    /// <summary>
    /// Cambia el estado del pedido (recibido → confirmado → preparando →
    /// en_camino → entregado, o cancelado desde cualquiera de los anteriores).
    /// Al confirmar descuenta stock; al cancelar un pedido ya confirmado lo
    /// revierte (ambos vía trigger en la base).
    /// </summary>
    [HttpPatch("{id:guid}/estado")]
    [Authorize(Policy = "AdminOVendedorODelivery")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDetalleDto>> CambiarEstado(
        Guid id, [FromBody] CambiarEstadoPedidoRequestDto request, CancellationToken ct)
    {
        var pedido = await _pedidoService.CambiarEstadoAsync(id, request.Estado, request.Observacion, User, ct);
        return Ok(pedido);
    }
}
