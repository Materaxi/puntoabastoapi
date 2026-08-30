using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.NotasVenta;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/notas-venta")]
[Authorize(Policy = "AdminOVendedor")]
[Produces("application/json")]
public class NotasVentaController : ControllerBase
{
    private readonly INotaVentaService _notaVentaService;

    public NotasVentaController(INotaVentaService notaVentaService)
    {
        _notaVentaService = notaVentaService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<NotaVentaListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<NotaVentaListItemDto>>> Buscar(
        [FromQuery] string? estado,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _notaVentaService.BuscarAsync(estado, desde, hasta, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotaVentaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotaVentaDetalleDto>> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var nota = await _notaVentaService.ObtenerPorIdAsync(id, ct);
        return Ok(nota);
    }

    /// <summary>
    /// Emite la nota de venta de un pedido (numeración NV-0001 vía trigger). El
    /// pedido debe estar confirmado o en un estado posterior, y no cancelado;
    /// un pedido solo puede tener una nota de venta (pedido_id es UNIQUE).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NotaVentaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NotaVentaDetalleDto>> Emitir(
        [FromBody] EmitirNotaVentaRequestDto request, CancellationToken ct)
    {
        var nota = await _notaVentaService.EmitirAsync(request, User, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = nota.Id }, nota);
    }

    /// <summary>Anula la nota (no revierte stock ni toca el pedido: eso se maneja aparte, cancelando el pedido).</summary>
    [HttpPatch("{id:guid}/anular")]
    [ProducesResponseType(typeof(NotaVentaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NotaVentaDetalleDto>> Anular(
        Guid id, [FromBody] AnularNotaVentaRequestDto request, CancellationToken ct)
    {
        var nota = await _notaVentaService.AnularAsync(id, request.Observaciones, User, ct);
        return Ok(nota);
    }
}
