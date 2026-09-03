using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("pedidos")]
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
        [FromQuery] bool? pagado,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _pedidoService.BuscarAsync(estado, clienteId, pagado, desde, hasta, page, pageSize, ct);
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

    /// <summary>Marca/desmarca el pago. Independiente del estado de entrega:
    /// un pedido puede estar "entregado" y sin pagar todavía.</summary>
    [HttpPatch("{id:guid}/pago")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDetalleDto>> ActualizarPago(
        Guid id, [FromBody] ActualizarPagoRequestDto request, CancellationToken ct)
    {
        var pedido = await _pedidoService.ActualizarPagoAsync(id, request.Pagado, request.MetodoPago, User, ct);
        return Ok(pedido);
    }

    /// <summary>Marca/desmarca el pedido como facturado. Recalcula Total (+16% IVA si
    /// facturado=true). Bloqueado si el pedido está cancelado o ya pagado.</summary>
    [HttpPatch("{id:guid}/facturado")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDetalleDto>> ActualizarFacturado(
        Guid id, [FromBody] ActualizarFacturadoRequestDto request, CancellationToken ct)
    {
        var pedido = await _pedidoService.ActualizarFacturadoAsync(id, request.Facturado, ct);
        return Ok(pedido);
    }

    /// <summary>Corrige el precio de un ítem (cliente con precio diferenciado).</summary>
    [HttpPatch("{id:guid}/items/{itemId:guid}/precio")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDetalleDto>> ActualizarPrecioItem(
        Guid id, Guid itemId, [FromBody] ActualizarPrecioItemRequestDto request, CancellationToken ct)
    {
        var pedido = await _pedidoService.ActualizarPrecioItemAsync(id, itemId, request.PrecioUnit, User, ct);
        return Ok(pedido);
    }
}
