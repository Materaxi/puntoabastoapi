using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Inventario;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize(Policy = "AdminOVendedor")]
[Produces("application/json")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _inventarioService;

    public InventarioController(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<InventarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<InventarioDto>>> Buscar(
        [FromQuery] bool? alertaActiva,
        [FromQuery] Guid? productoId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _inventarioService.BuscarAsync(alertaActiva, productoId, q, page, pageSize, ct);
        return Ok(resultado);
    }

    /// <summary>Atajo para el panel del dueño: solo lo que está bajo el stock mínimo.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyList<InventarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InventarioDto>>> Alertas(CancellationToken ct)
    {
        var alertas = await _inventarioService.ListarAlertasAsync(ct);
        return Ok(alertas);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventarioDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var inventario = await _inventarioService.ObtenerPorIdAsync(id, ct);
        return Ok(inventario);
    }

    [HttpGet("{id:guid}/movimientos")]
    [ProducesResponseType(typeof(PagedResultDto<MovimientoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<MovimientoDto>>> Movimientos(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var resultado = await _inventarioService.ListarMovimientosAsync(id, page, pageSize, ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Registra una entrada (compra) o un ajuste manual (merma, producto en mal
    /// estado, conteo físico). "salida" no es válido acá: la genera solo el
    /// trigger de Postgres al confirmar un pedido.
    /// </summary>
    [HttpPost("{id:guid}/movimientos")]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventarioDto>> RegistrarMovimiento(
        Guid id, [FromBody] RegistrarMovimientoRequestDto request, CancellationToken ct)
    {
        var inventario = await _inventarioService.RegistrarMovimientoAsync(id, request, User, ct);
        return Ok(inventario);
    }

    [HttpPut("{id:guid}/umbrales")]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventarioDto>> ActualizarUmbrales(
        Guid id, [FromBody] ActualizarUmbralesRequestDto request, CancellationToken ct)
    {
        var inventario = await _inventarioService.ActualizarUmbralesAsync(id, request, ct);
        return Ok(inventario);
    }
}
