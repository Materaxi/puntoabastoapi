using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Inventario;

namespace PuntoAbasto.Api.Services;

public interface IInventarioService
{
    Task<PagedResultDto<InventarioDto>> BuscarAsync(
        bool? alertaActiva, Guid? productoId, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<InventarioDto>> ListarAlertasAsync(CancellationToken ct);

    Task<InventarioDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResultDto<MovimientoDto>> ListarMovimientosAsync(Guid id, int page, int pageSize, CancellationToken ct);

    Task<InventarioDto> RegistrarMovimientoAsync(
        Guid id, RegistrarMovimientoRequestDto request, ClaimsPrincipal usuario, CancellationToken ct);

    Task<InventarioDto> ActualizarUmbralesAsync(Guid id, ActualizarUmbralesRequestDto request, CancellationToken ct);
}
