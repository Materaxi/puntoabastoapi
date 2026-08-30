using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public interface IClienteRepository
{
    Task<(IReadOnlyList<Cliente> Items, int TotalCount)> BuscarAsync(ClienteFiltro filtro, CancellationToken ct);

    Task<Cliente?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Cliente?> ObtenerPorTelefonoAsync(string telefono, CancellationToken ct);

    Task AgregarAsync(Cliente cliente, CancellationToken ct);
}
