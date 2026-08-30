using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _db;

    public PedidoRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Usuario)
            .Include(p => p.Items)
            .Include(p => p.HistorialEstados.OrderBy(h => h.CreatedAt))
                .ThenInclude(h => h.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Pedido> Items, int TotalCount)> BuscarAsync(PedidoFiltro filtro, CancellationToken ct)
    {
        var query = _db.Pedidos.Include(p => p.Cliente).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            query = query.Where(p => p.Estado == filtro.Estado);
        }

        if (filtro.ClienteId is not null)
        {
            query = query.Where(p => p.ClienteId == filtro.ClienteId);
        }

        if (filtro.Desde is not null)
        {
            query = query.Where(p => p.FechaPedido >= filtro.Desde);
        }

        if (filtro.Hasta is not null)
        {
            query = query.Where(p => p.FechaPedido <= filtro.Hasta);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.FechaPedido)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AgregarAsync(Pedido pedido, CancellationToken ct) =>
        await _db.Pedidos.AddAsync(pedido, ct);
}
