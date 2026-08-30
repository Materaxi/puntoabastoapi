using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public class NotaVentaRepository : INotaVentaRepository
{
    private readonly AppDbContext _db;

    public NotaVentaRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<NotaVenta?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        _db.NotasVenta
            .Include(n => n.Pedido).ThenInclude(p => p!.Cliente)
            .Include(n => n.Pedido).ThenInclude(p => p!.Items)
            .Include(n => n.Usuario)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<(IReadOnlyList<NotaVenta> Items, int TotalCount)> BuscarAsync(NotaVentaFiltro filtro, CancellationToken ct)
    {
        var query = _db.NotasVenta
            .Include(n => n.Pedido).ThenInclude(p => p!.Cliente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            query = query.Where(n => n.Estado == filtro.Estado);
        }

        if (filtro.Desde is not null)
        {
            query = query.Where(n => n.FechaEmision >= filtro.Desde);
        }

        if (filtro.Hasta is not null)
        {
            query = query.Where(n => n.FechaEmision <= filtro.Hasta);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.FechaEmision)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AgregarAsync(NotaVenta notaVenta, CancellationToken ct) =>
        await _db.NotasVenta.AddAsync(notaVenta, ct);
}
