using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Clientes;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>
/// Portal B2B: clientes-empresa autenticados (grupo selecto, alta manual
/// desde el admin — ver ClientesController.HabilitarAccesoPortal). El
/// catálogo (categorías/productos) no tiene endpoints acá porque
/// GET /api/categorias y GET /api/productos ya son [AllowAnonymous] y el
/// portal los consume directo, sin duplicar nada.
/// </summary>
[ApiController]
[Route("api/portal")]
[Produces("application/json")]
[Authorize(Policy = "ClientePortal")]
public class PortalController : ControllerBase
{
    private readonly IClientePortalContext _clientePortalContext;
    private readonly IClienteService _clienteService;
    private readonly IPedidoService _pedidoService;

    public PortalController(IClientePortalContext clientePortalContext, IClienteService clienteService, IPedidoService pedidoService)
    {
        _clientePortalContext = clientePortalContext;
        _clienteService = clienteService;
        _pedidoService = pedidoService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ClienteDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> Me(CancellationToken ct)
    {
        var cliente = await _clientePortalContext.ObtenerClienteActualAsync(User, ct);
        var detalle = await _clienteService.ObtenerPorIdAsync(cliente.Id, ct);
        return Ok(detalle);
    }

    /// <summary>Crea un pedido para el cliente autenticado — nunca resuelto por
    /// teléfono ni por un id que venga en el body, siempre por el JWT.</summary>
    [HttpPost("pedidos")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PedidoDetalleDto>> CrearPedido([FromBody] CrearPedidoPortalRequestDto request, CancellationToken ct)
    {
        var cliente = await _clientePortalContext.ObtenerClienteActualAsync(User, ct);
        var pedido = await _pedidoService.CrearParaClienteAsync(cliente.Id, request, ct);
        return CreatedAtAction(nameof(ObtenerPedido), new { id = pedido.Id }, pedido);
    }

    /// <summary>Historial del cliente autenticado. clienteId se fija acá desde el
    /// JWT, nunca desde query string — a diferencia de GET /api/pedidos (staff),
    /// este endpoint jamás debe poder listar pedidos de otro cliente.</summary>
    [HttpGet("pedidos")]
    [ProducesResponseType(typeof(PagedResultDto<PedidoListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PedidoListItemDto>>> MisPedidos(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var cliente = await _clientePortalContext.ObtenerClienteActualAsync(User, ct);
        var resultado = await _pedidoService.BuscarAsync(
            estado: null, clienteId: cliente.Id, pagado: null, desde: null, hasta: null, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("pedidos/{id:guid}")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDetalleDto>> ObtenerPedido(Guid id, CancellationToken ct)
    {
        var cliente = await _clientePortalContext.ObtenerClienteActualAsync(User, ct);
        var pedido = await _pedidoService.ObtenerPorIdAsync(id, ct);

        // 404 y no 403 a propósito: no confirmar que un pedido de otro cliente existe.
        if (pedido.Cliente.Id != cliente.Id)
        {
            throw new KeyNotFoundException($"No existe el pedido {id}.");
        }

        return Ok(pedido);
    }
}
