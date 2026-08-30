using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;

    public ClienteRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Cliente> Items, int TotalCount)> BuscarAsync(ClienteFiltro filtro, CancellationToken ct)
    {
        var query = _db.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Nombre, $"%{filtro.Busqueda}%") ||
                EF.Functions.ILike(c.Telefono, $"%{filtro.Busqueda}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Cliente?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObtenerPorTelefonoAsync(string telefono, CancellationToken ct) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == telefono, ct);

    public async Task AgregarAsync(Cliente cliente, CancellationToken ct) =>
        await _db.Clientes.AddAsync(cliente, ct);
}
