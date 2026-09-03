using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Compras;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

/// <summary>Registro de compras a proveedor (costo + suma al stock), para el
/// costeo de utilidades en /api/reportes/costeo.</summary>
[ApiController]
[Route("api/compras")]
[Authorize(Policy = "AdminOVendedor")]
[Produces("application/json")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;

    public ComprasController(ICompraService compraService)
    {
        _compraService = compraService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<CompraDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<CompraDto>>> Buscar(
        [FromQuery] Guid? productoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var resultado = await _compraService.BuscarAsync(productoId, desde, hasta, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompraDto>> Registrar([FromBody] RegistrarCompraRequestDto request, CancellationToken ct)
    {
        var compra = await _compraService.RegistrarAsync(request, User, ct);
        return CreatedAtAction(nameof(Buscar), compra);
    }
}
