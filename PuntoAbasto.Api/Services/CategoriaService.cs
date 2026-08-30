using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Catalogo;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _db;

    public CategoriaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct) =>
        await _db.Categorias
            .OrderBy(c => c.Orden)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Slug, c.Emoji, c.Orden))
            .ToListAsync(ct);

    public async Task<CategoriaDto> CrearAsync(CategoriaRequestDto request, CancellationToken ct)
    {
        var categoria = new Categoria
        {
            Nombre = request.Nombre,
            Slug = request.Slug,
            Emoji = request.Emoji,
            Orden = request.Orden
        };

        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync(ct);

        return MapToDto(categoria);
    }

    public async Task<CategoriaDto> ActualizarAsync(int id, CategoriaRequestDto request, CancellationToken ct)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe la categoría {id}.");

        categoria.Nombre = request.Nombre;
        categoria.Slug = request.Slug;
        categoria.Emoji = request.Emoji;
        categoria.Orden = request.Orden;

        await _db.SaveChangesAsync(ct);

        return MapToDto(categoria);
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe la categoría {id}.");

        // Si tiene productos, la FK RESTRICT de productos.categoria_id hace que
        // Postgres rechace el DELETE; el middleware lo traduce a 409 solo.
        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync(ct);
    }

    private static CategoriaDto MapToDto(Categoria categoria) =>
        new(categoria.Id, categoria.Nombre, categoria.Slug, categoria.Emoji, categoria.Orden);
}
