using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Catalogo;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    // ── Catálogo público (carrito web) ──────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoPublicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoPublicoDto>>> ListarPublico(
        [FromQuery] int? categoriaId, CancellationToken ct)
    {
        var productos = await _productoService.ListarPublicoAsync(categoriaId, ct);
        return Ok(productos);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductoPublicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoPublicoDto>> ObtenerPublico(Guid id, CancellationToken ct)
    {
        var producto = await _productoService.ObtenerPublicoPorIdAsync(id, ct);
        return Ok(producto);
    }

    // ── Panel interno (admin/vendedor) ──────────────────────────────

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(PagedResultDto<ProductoInternoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProductoInternoDto>>> Buscar(
        [FromQuery] int? categoriaId,
        [FromQuery] bool? activo,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _productoService.BuscarAsync(categoriaId, activo, q, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("admin/{id:guid}")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoInternoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoInternoDto>> ObtenerInterno(Guid id, CancellationToken ct)
    {
        var producto = await _productoService.ObtenerInternoPorIdAsync(id, ct);
        return Ok(producto);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoInternoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductoInternoDto>> Crear([FromBody] CrearProductoRequestDto request, CancellationToken ct)
    {
        var producto = await _productoService.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerInterno), new { id = producto.Id }, producto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoInternoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoInternoDto>> Actualizar(
        Guid id, [FromBody] ActualizarProductoRequestDto request, CancellationToken ct)
    {
        var producto = await _productoService.ActualizarAsync(id, request, ct);
        return Ok(producto);
    }

    /// <summary>Toggle rápido de "agotado" sin pasar por el formulario completo.</summary>
    [HttpPatch("{id:guid}/disponibilidad")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoInternoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoInternoDto>> ActualizarDisponibilidad(
        Guid id, [FromBody] ActualizarDisponibilidadRequestDto request, CancellationToken ct)
    {
        var producto = await _productoService.ActualizarDisponibilidadAsync(id, request.Disponible, ct);
        return Ok(producto);
    }

    /// <summary>Soft delete (activo=false): nunca borra físico para no perder historial.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _productoService.EliminarAsync(id, ct);
        return NoContent();
    }

    // ── Unidades de venta ────────────────────────────────────────────

    [HttpPost("{id:guid}/unidades")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoUnidadInternaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductoUnidadInternaDto>> AgregarUnidad(
        Guid id, [FromBody] CrearProductoUnidadRequestDto request, CancellationToken ct)
    {
        var unidad = await _productoService.AgregarUnidadAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, unidad);
    }

    [HttpPut("{id:guid}/unidades/{unidadId:guid}")]
    [Authorize(Policy = "AdminOVendedor")]
    [ProducesResponseType(typeof(ProductoUnidadInternaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoUnidadInternaDto>> ActualizarUnidad(
        Guid id, Guid unidadId, [FromBody] ActualizarUnidadRequestDto request, CancellationToken ct)
    {
        var unidad = await _productoService.ActualizarUnidadAsync(id, unidadId, request, ct);
        return Ok(unidad);
    }

    /// <summary>Falla con 409 si la unidad tiene pedidos o movimientos de inventario asociados.</summary>
    [HttpDelete("{id:guid}/unidades/{unidadId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarUnidad(Guid id, Guid unidadId, CancellationToken ct)
    {
        await _productoService.EliminarUnidadAsync(id, unidadId, ct);
        return NoContent();
    }
}
