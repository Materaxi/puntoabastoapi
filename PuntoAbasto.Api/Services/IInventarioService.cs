using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Inventario;

namespace PuntoAbasto.Api.Services;

public interface IInventarioService
{
    Task<PagedResultDto<InventarioDto>> BuscarAsync(
        bool? alertaActiva, Guid? productoId, string? q, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<InventarioDto>> ListarAlertasAsync(CancellationToken ct);

    Task<InventarioDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResultDto<MovimientoDto>> ListarMovimientosAsync(Guid id, int page, int pageSize, CancellationToken ct);

    Task<InventarioDto> RegistrarMovimientoAsync(
        Guid id, RegistrarMovimientoRequestDto request, ClaimsPrincipal usuario, CancellationToken ct);

    Task<InventarioDto> ActualizarUmbralesAsync(Guid id, ActualizarUmbralesRequestDto request, CancellationToken ct);

    /// <summary>Fija el stock actual a un valor absoluto (no un delta) — para el módulo
    /// simplificado de almacén: "queda tanto de este producto". Internamente lo registra
    /// como un movimiento "ajuste" (mismo patrón de auditoría que RegistrarMovimientoAsync),
    /// calculando la diferencia contra el stock anterior. No hace nada si no cambia.</summary>
    Task<InventarioDto> ActualizarStockAsync(Guid id, decimal stockActual, ClaimsPrincipal usuario, CancellationToken ct);
}
