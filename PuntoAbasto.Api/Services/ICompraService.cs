using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Compras;

namespace PuntoAbasto.Api.Services;

public interface ICompraService
{
    /// <summary>Registra una compra a proveedor y suma la cantidad al stock (genera
    /// un movimiento "entrada" en INVENTARIO_MOVIMIENTOS, mismo patrón de auditoría
    /// que el resto de movimientos).</summary>
    Task<CompraDto> RegistrarAsync(RegistrarCompraRequestDto request, ClaimsPrincipal usuario, CancellationToken ct);

    Task<PagedResultDto<CompraDto>> BuscarAsync(
        Guid? productoId, DateOnly? desde, DateOnly? hasta, int page, int pageSize, CancellationToken ct);
}
