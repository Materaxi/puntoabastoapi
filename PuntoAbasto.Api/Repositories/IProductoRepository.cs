using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Repositories;

public interface IProductoRepository
{
    /// <summary>Catálogo público: solo activo=true, ordenado, con unidades e inventario.</summary>
    Task<IReadOnlyList<Producto>> ListarPublicoAsync(int? categoriaId, CancellationToken ct);

    Task<Producto?> ObtenerPublicoPorIdAsync(Guid id, CancellationToken ct);

    Task<Producto?> ObtenerConDetalleAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<Producto> Items, int TotalCount)> BuscarAsync(ProductoFiltro filtro, CancellationToken ct);

    Task AgregarAsync(Producto producto, CancellationToken ct);
}
