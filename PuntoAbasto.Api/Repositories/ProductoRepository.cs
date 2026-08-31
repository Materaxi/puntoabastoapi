using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _db;

    public ProductoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Producto>> ListarPublicoAsync(int? categoriaId, CancellationToken ct)
    {
        var query = _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Unidades).ThenInclude(u => u.Inventario)
            .Where(p => p.Activo)
            .AsQueryable();

        if (categoriaId is not null)
        {
            query = query.Where(p => p.CategoriaId == categoriaId);
        }

        return await query
            .OrderBy(p => p.Categoria!.Orden).ThenBy(p => p.Orden)
            .ToListAsync(ct);
    }

    public Task<Producto?> ObtenerPublicoPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Unidades).ThenInclude(u => u.Inventario)
            .FirstOrDefaultAsync(p => p.Id == id && p.Activo, ct);

    public Task<Producto?> ObtenerConDetalleAsync(Guid id, CancellationToken ct) =>
        _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Unidades).ThenInclude(u => u.Inventario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Producto> Items, int TotalCount)> BuscarAsync(ProductoFiltro filtro, CancellationToken ct)
    {
        var query = _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Unidades).ThenInclude(u => u.Inventario)
            .AsQueryable();

        if (filtro.CategoriaId is not null)
        {
            query = query.Where(p => p.CategoriaId == filtro.CategoriaId);
        }

        if (filtro.Activo is not null)
        {
            query = query.Where(p => p.Activo == filtro.Activo);
        }

        if (filtro.StockBajo == true)
        {
            query = query.Where(p => p.Unidades.Any(u => u.Inventario != null && u.Inventario.AlertaActiva));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            query = query.Where(p => EF.Functions.ILike(p.Nombre, $"%{filtro.Busqueda}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Categoria!.Orden).ThenBy(p => p.Orden)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AgregarAsync(Producto producto, CancellationToken ct) =>
        await _db.Productos.AddAsync(producto, ct);
}
