using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public interface INotaVentaRepository
{
    Task<NotaVenta?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<NotaVenta> Items, int TotalCount)> BuscarAsync(NotaVentaFiltro filtro, CancellationToken ct);

    Task AgregarAsync(NotaVenta notaVenta, CancellationToken ct);
}
