using PuntoAbasto.Api.DTOs.Catalogo;

namespace PuntoAbasto.Api.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct);
    Task<CategoriaDto> CrearAsync(CategoriaRequestDto request, CancellationToken ct);
    Task<CategoriaDto> ActualizarAsync(int id, CategoriaRequestDto request, CancellationToken ct);
    Task EliminarAsync(int id, CancellationToken ct);
}
