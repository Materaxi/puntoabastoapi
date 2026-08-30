using System.Security.Claims;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;

namespace PuntoAbasto.Api.Services;

public interface IPedidoService
{
    Task<PedidoDetalleDto> CrearAsync(CrearPedidoRequestDto request, CancellationToken ct);

    Task<PedidoDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResultDto<PedidoListItemDto>> BuscarAsync(
        string? estado, Guid? clienteId, DateTimeOffset? desde, DateTimeOffset? hasta,
        int page, int pageSize, CancellationToken ct);

    Task<PedidoDetalleDto> CambiarEstadoAsync(
        Guid id, string nuevoEstado, string? observacion, ClaimsPrincipal usuario, CancellationToken ct);
}
