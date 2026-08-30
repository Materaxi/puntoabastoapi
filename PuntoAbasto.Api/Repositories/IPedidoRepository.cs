using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public interface IPedidoRepository
{
    /// <summary>Pedido completo (cliente, usuario, items, historial de estados) o null si no existe.</summary>
    Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<Pedido> Items, int TotalCount)> BuscarAsync(PedidoFiltro filtro, CancellationToken ct);

    Task AgregarAsync(Pedido pedido, CancellationToken ct);
}
