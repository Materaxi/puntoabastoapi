using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Inventario;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>
/// Módulo simplificado de almacén: buscar un producto y fijar cuánto queda,
/// sin precios/imágenes/umbrales/tipos de movimiento — eso vive en /api/inventario
/// y /api/productos, para admin/vendedor. Autorización propia (no reusa
/// InventarioController) porque el rol "almacenero" no debe entrar a esos otros
/// endpoints.
/// </summary>
[ApiController]
[Route("api/almacen")]
[Authorize(Policy = "AdminOAlmacenero")]
[Produces("application/json")]
public class AlmacenController : ControllerBase
{
    private readonly IInventarioService _inventarioService;

    public AlmacenController(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService;
    }

    /// <summary>Lista producto + unidad + stock actual, buscable por nombre de producto.</summary>
    [HttpGet("productos")]
    [ProducesResponseType(typeof(PagedResultDto<InventarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<InventarioDto>>> Buscar(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var resultado = await _inventarioService.BuscarAsync(alertaActiva: null, productoId: null, q, page, pageSize, ct);
        return Ok(resultado);
    }

    /// <summary>Fija el stock actual a un valor absoluto (el conteo físico), no un delta.</summary>
    [HttpPut("{id:guid}/stock")]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventarioDto>> ActualizarStock(
        Guid id, [FromBody] ActualizarStockRequestDto request, CancellationToken ct)
    {
        var inventario = await _inventarioService.ActualizarStockAsync(id, request.StockActual, User, ct);
        return Ok(inventario);
    }
}
