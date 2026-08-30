using PuntoAbasto.Api.DTOs.Clientes;
using PuntoAbasto.Api.DTOs.Common;

namespace PuntoAbasto.Api.Services;

public interface IClienteService
{
    Task<PagedResultDto<ClienteListItemDto>> BuscarAsync(string? busqueda, int page, int pageSize, CancellationToken ct);
    Task<ClienteDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct);
    Task<ClienteDetalleDto> CrearAsync(CrearClienteRequestDto request, CancellationToken ct);
    Task<ClienteDetalleDto> ActualizarAsync(Guid id, ActualizarClienteRequestDto request, CancellationToken ct);
}
