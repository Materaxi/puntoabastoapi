using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.NotasVenta;

namespace PuntoAbasto.Api.Services;

public interface INotaVentaService
{
    Task<PagedResultDto<NotaVentaListItemDto>> BuscarAsync(
        string? estado, DateTimeOffset? desde, DateTimeOffset? hasta, int page, int pageSize, CancellationToken ct);

    Task<NotaVentaDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<NotaVentaDetalleDto> EmitirAsync(EmitirNotaVentaRequestDto request, ClaimsPrincipal usuario, CancellationToken ct);

    Task<NotaVentaDetalleDto> AnularAsync(Guid id, string observaciones, ClaimsPrincipal usuario, CancellationToken ct);
}
