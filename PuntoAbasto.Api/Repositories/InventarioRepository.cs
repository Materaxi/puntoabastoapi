using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public class InventarioRepository : IInventarioRepository
{
    private readonly AppDbContext _db;

    public InventarioRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Inventario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Inventarios
            .Include(i => i.ProductoUnidad).ThenInclude(u => u!.Producto)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<(IReadOnlyList<Inventario> Items, int TotalCount)> BuscarAsync(InventarioFiltro filtro, CancellationToken ct)
    {
        var query = _db.Inventarios
            .Include(i => i.ProductoUnidad).ThenInclude(u => u!.Producto)
            .AsQueryable();

        if (filtro.AlertaActiva is not null)
        {
            query = query.Where(i => i.AlertaActiva == filtro.AlertaActiva);
        }

        if (filtro.ProductoId is not null)
        {
            query = query.Where(i => i.ProductoUnidad!.ProductoId == filtro.ProductoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Q))
        {
            var patron = $"%{filtro.Q.Trim()}%";
            query = query.Where(i => EF.Functions.ILike(i.ProductoUnidad!.Producto!.Nombre, patron));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(i => i.ProductoUnidad!.Producto!.Nombre).ThenBy(i => i.ProductoUnidad!.Label)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Inventario>> ListarAlertasAsync(CancellationToken ct) =>
        await _db.Inventarios
            .Include(i => i.ProductoUnidad).ThenInclude(u => u!.Producto)
            .Where(i => i.AlertaActiva)
            .OrderBy(i => i.ProductoUnidad!.Producto!.Nombre)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<InventarioMovimiento> Items, int TotalCount)> ListarMovimientosAsync(
        Guid inventarioId, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.InventarioMovimientos
            .Include(m => m.Usuario)
            .Where(m => m.InventarioId == inventarioId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
