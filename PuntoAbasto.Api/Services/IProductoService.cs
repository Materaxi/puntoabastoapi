using PuntoAbasto.Api.DTOs.Catalogo;
using PuntoAbasto.Api.DTOs.Common;

namespace PuntoAbasto.Api.Services;

public interface IProductoService
{
    Task<IReadOnlyList<ProductoPublicoDto>> ListarPublicoAsync(int? categoriaId, CancellationToken ct);
    Task<ProductoPublicoDto> ObtenerPublicoPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResultDto<ProductoInternoDto>> BuscarAsync(
        int? categoriaId, bool? activo, string? busqueda, int page, int pageSize, CancellationToken ct);
    Task<ProductoInternoDto> ObtenerInternoPorIdAsync(Guid id, CancellationToken ct);
    Task<ProductoInternoDto> CrearAsync(CrearProductoRequestDto request, CancellationToken ct);
    Task<ProductoInternoDto> ActualizarAsync(Guid id, ActualizarProductoRequestDto request, CancellationToken ct);
    Task<ProductoInternoDto> ActualizarDisponibilidadAsync(Guid id, bool disponible, CancellationToken ct);
    Task EliminarAsync(Guid id, CancellationToken ct);

    Task<ProductoUnidadInternaDto> AgregarUnidadAsync(Guid productoId, CrearProductoUnidadRequestDto request, CancellationToken ct);
    Task<ProductoUnidadInternaDto> ActualizarUnidadAsync(
        Guid productoId, Guid unidadId, ActualizarUnidadRequestDto request, CancellationToken ct);
    Task EliminarUnidadAsync(Guid productoId, Guid unidadId, CancellationToken ct);
}
