using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Clientes;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.Models;
using PuntoAbasto.Api.Repositories;

namespace PuntoAbasto.Api.Services;

public class ClienteService : IClienteService
{
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;
    private readonly IClienteRepository _clienteRepository;

    public ClienteService(AppDbContext db, IClienteRepository clienteRepository)
    {
        _db = db;
        _clienteRepository = clienteRepository;
    }

    public async Task<PagedResultDto<ClienteListItemDto>> BuscarAsync(string? busqueda, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new ClienteFiltro(busqueda, page, pageSize);
        var (clientes, totalCount) = await _clienteRepository.BuscarAsync(filtro, ct);

        return new PagedResultDto<ClienteListItemDto>(clientes.Select(MapToListItemDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<ClienteDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var cliente = await _clienteRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {id}.");
        return MapToDetalleDto(cliente);
    }

    public async Task<ClienteDetalleDto> CrearAsync(CrearClienteRequestDto request, CancellationToken ct)
    {
        var existente = await _clienteRepository.ObtenerPorTelefonoAsync(request.Telefono, ct);
        if (existente is not null)
        {
            throw new ArgumentException($"Ya existe un cliente con el teléfono {request.Telefono}.");
        }

        var cliente = new Cliente
        {
            Nombre = request.Nombre,
            Telefono = request.Telefono,
            Direccion = request.Direccion,
            ReferenciaDireccion = request.ReferenciaDireccion,
            UbicacionGps = request.UbicacionGps,
            Email = request.Email
        };

        await _clienteRepository.AgregarAsync(cliente, ct);
        await _db.SaveChangesAsync(ct);

        return MapToDetalleDto(cliente);
    }

    public async Task<ClienteDetalleDto> ActualizarAsync(Guid id, ActualizarClienteRequestDto request, CancellationToken ct)
    {
        var cliente = await _clienteRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {id}.");

        if (!string.Equals(cliente.Telefono, request.Telefono, StringComparison.Ordinal))
        {
            var otro = await _clienteRepository.ObtenerPorTelefonoAsync(request.Telefono, ct);
            if (otro is not null && otro.Id != id)
            {
                throw new ArgumentException($"Ya existe un cliente con el teléfono {request.Telefono}.");
            }
        }

        cliente.Nombre = request.Nombre;
        cliente.Telefono = request.Telefono;
        cliente.Direccion = request.Direccion;
        cliente.ReferenciaDireccion = request.ReferenciaDireccion;
        cliente.UbicacionGps = request.UbicacionGps;
        cliente.Email = request.Email;

        await _db.SaveChangesAsync(ct);

        return MapToDetalleDto(cliente);
    }

    private static ClienteListItemDto MapToListItemDto(Cliente cliente) => new(
        cliente.Id, cliente.Nombre, cliente.Telefono, cliente.Direccion,
        cliente.TotalPedidos, cliente.TotalGastado, cliente.CreatedAt);

    private static ClienteDetalleDto MapToDetalleDto(Cliente cliente) => new(
        cliente.Id, cliente.Nombre, cliente.Telefono, cliente.Direccion,
        cliente.ReferenciaDireccion, cliente.UbicacionGps, cliente.Email, cliente.TotalPedidos, cliente.TotalGastado,
        cliente.AccesoPortal, cliente.CreatedAt, cliente.UpdatedAt);
}
