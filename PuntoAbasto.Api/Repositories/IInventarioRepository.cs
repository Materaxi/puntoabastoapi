using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public interface IInventarioRepository
{
    Task<Inventario?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<Inventario> Items, int TotalCount)> BuscarAsync(InventarioFiltro filtro, CancellationToken ct);

    Task<IReadOnlyList<Inventario>> ListarAlertasAsync(CancellationToken ct);

    Task<(IReadOnlyList<InventarioMovimiento> Items, int TotalCount)> ListarMovimientosAsync(
        Guid inventarioId, int page, int pageSize, CancellationToken ct);
}
