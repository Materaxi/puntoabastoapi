using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAbasto.Api.DTOs.Catalogo;
using PuntoAbasto.Api.Services;

namespace PuntoAbasto.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Produces("application/json")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(CancellationToken ct)
    {
        var categorias = await _categoriaService.ListarAsync(ct);
        return Ok(categorias);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoriaDto>> Crear([FromBody] CategoriaRequestDto request, CancellationToken ct)
    {
        var categoria = await _categoriaService.CrearAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, categoria);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoriaDto>> Actualizar(int id, [FromBody] CategoriaRequestDto request, CancellationToken ct)
    {
        var categoria = await _categoriaService.ActualizarAsync(id, request, ct);
        return Ok(categoria);
    }

    /// <summary>Falla con 409 si la categoría todavía tiene productos (FK RESTRICT).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _categoriaService.EliminarAsync(id, ct);
        return NoContent();
    }
}
