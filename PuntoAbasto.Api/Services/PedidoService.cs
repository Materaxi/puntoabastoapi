using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;
using PuntoAbasto.Api.Repositories;

namespace PuntoAbasto.Api.Services;

public class PedidoService : IPedidoService
{
    private static readonly HashSet<string> OrigenesValidos = ["whatsapp", "web", "telefono"];
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;
    private readonly IPedidoRepository _pedidoRepository;

    public PedidoService(AppDbContext db, IPedidoRepository pedidoRepository)
    {
        _db = db;
        _pedidoRepository = pedidoRepository;
    }

    public async Task<PedidoDetalleDto> CrearAsync(CrearPedidoRequestDto request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("El pedido debe tener al menos un item.");
        }

        var origen = string.IsNullOrWhiteSpace(request.Origen) ? "web" : request.Origen.Trim().ToLowerInvariant();
        if (!OrigenesValidos.Contains(origen))
        {
            throw new ArgumentException($"Origen '{request.Origen}' no es válido. Debe ser: {string.Join(", ", OrigenesValidos)}.");
        }

        // El cliente no tiene cuenta: se identifica por teléfono. Si ya existe,
        // reaprovechamos la fila y refrescamos los datos de contacto/entrega con
        // lo que vino en este pedido (puede haber cambiado de dirección, etc.).
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == request.ClienteTelefono, ct);
        if (cliente is null)
        {
            cliente = new Cliente
            {
                Nombre = request.ClienteNombre,
                Telefono = request.ClienteTelefono,
                Direccion = request.ClienteDireccion,
                ReferenciaDireccion = request.ClienteReferenciaDireccion,
                Email = request.ClienteEmail
            };
            _db.Clientes.Add(cliente);
        }
        else
        {
            cliente.Nombre = request.ClienteNombre;
            cliente.Direccion = request.ClienteDireccion;
            cliente.ReferenciaDireccion = request.ClienteReferenciaDireccion;
            cliente.Email = request.ClienteEmail;
        }

        var unidadIds = request.Items.Select(i => i.ProductoUnidadId).Distinct().ToList();
        var unidades = await _db.ProductoUnidades
            .Include(u => u.Producto)
            .Where(u => unidadIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var faltantes = unidadIds.Where(id => !unidades.ContainsKey(id)).ToList();
        if (faltantes.Count > 0)
        {
            throw new ArgumentException($"No existen las unidades de producto: {string.Join(", ", faltantes)}.");
        }

        var items = new List<PedidoItem>();
        foreach (var itemRequest in request.Items)
        {
            var unidad = unidades[itemRequest.ProductoUnidadId];
            var producto = unidad.Producto!;

            if (!producto.Activo || !producto.Disponible)
            {
                throw new InvalidOperationException($"'{producto.Nombre}' no está disponible actualmente.");
            }

            items.Add(new PedidoItem
            {
                ProductoUnidadId = unidad.Id,
                ProductoNombre = producto.Nombre,
                UnidadLabel = unidad.Label,
                PrecioUnit = unidad.Precio,
                Cantidad = itemRequest.Cantidad,
                Subtotal = unidad.Precio * itemRequest.Cantidad
            });
        }

        var subtotalPedido = items.Sum(i => i.Subtotal);
        if (request.Descuento > subtotalPedido)
        {
            throw new ArgumentException("El descuento no puede ser mayor al subtotal del pedido.");
        }

        var pedido = new Pedido
        {
            Cliente = cliente,
            Estado = "recibido",
            Origen = origen,
            Subtotal = subtotalPedido,
            Descuento = request.Descuento,
            Total = subtotalPedido - request.Descuento,
            Notas = request.Notas,
            FechaEntregaEst = DateTimeOffset.UtcNow.AddHours(24),
            Items = items
        };

        await _pedidoRepository.AgregarAsync(pedido, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedido.Id, ct);
    }

    public async Task<PedidoDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");

        return MapToDetalleDto(pedido);
    }

    public async Task<PagedResultDto<PedidoListItemDto>> BuscarAsync(
        string? estado, Guid? clienteId, DateTimeOffset? desde, DateTimeOffset? hasta,
        int page, int pageSize, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(estado) && !PedidoEstadoTransiciones.EstadosValidos.Contains(estado))
        {
            throw new ArgumentException($"Estado '{estado}' no es válido.");
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new PedidoFiltro(estado, clienteId, desde, hasta, page, pageSize);
        var (pedidos, totalCount) = await _pedidoRepository.BuscarAsync(filtro, ct);

        var items = pedidos.Select(MapToListItemDto).ToList();
        return new PagedResultDto<PedidoListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<PedidoDetalleDto> CambiarEstadoAsync(
        Guid id, string nuevoEstado, string? observacion, ClaimsPrincipal usuario, CancellationToken ct)
    {
        if (!PedidoEstadoTransiciones.EstadosValidos.Contains(nuevoEstado))
        {
            throw new ArgumentException($"Estado '{nuevoEstado}' no es válido.");
        }

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");

        if (!PedidoEstadoTransiciones.EsTransicionValida(pedido.Estado, nuevoEstado))
        {
            throw new InvalidOperationException(
                $"No se puede pasar el pedido {pedido.Numero} de '{pedido.Estado}' a '{nuevoEstado}'.");
        }

        var usuarioId = usuario.GetUsuarioId();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        // El trigger de Postgres se encarga de: descontar/revertir stock,
        // registrar la fila en pedido_estados y actualizar totales del
        // cliente al entregar. Acá solo cambiamos el estado y, si vino,
        // adjuntamos la observación a la fila de historial recién creada.
        pedido.Estado = nuevoEstado;
        pedido.UsuarioId = usuarioId;
        if (nuevoEstado == "entregado")
        {
            pedido.FechaEntregaReal = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(observacion))
        {
            var historialReciente = await _db.PedidoEstados
                .Where(pe => pe.PedidoId == id)
                .OrderByDescending(pe => pe.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (historialReciente is not null)
            {
                historialReciente.Observacion = observacion;
                await _db.SaveChangesAsync(ct);
            }
        }

        await transaction.CommitAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    private static PedidoListItemDto MapToListItemDto(Pedido pedido) => new(
        pedido.Id,
        pedido.Numero,
        pedido.Cliente!.Nombre,
        pedido.Cliente.Telefono,
        pedido.Estado,
        pedido.Origen,
        pedido.Total,
        pedido.FechaPedido);

    private static PedidoDetalleDto MapToDetalleDto(Pedido pedido) => new(
        pedido.Id,
        pedido.Numero,
        new ClienteResumenDto(
            pedido.Cliente!.Id,
            pedido.Cliente.Nombre,
            pedido.Cliente.Telefono,
            pedido.Cliente.Direccion,
            pedido.Cliente.ReferenciaDireccion),
        pedido.Usuario?.Nombre,
        pedido.Estado,
        pedido.Origen,
        pedido.Subtotal,
        pedido.Descuento,
        pedido.Total,
        pedido.Notas,
        pedido.FechaPedido,
        pedido.FechaEntregaEst,
        pedido.FechaEntregaReal,
        pedido.Items.Select(i => new PedidoItemDto(
            i.Id, i.ProductoUnidadId, i.ProductoNombre, i.UnidadLabel, i.PrecioUnit, i.Cantidad, i.Subtotal)).ToList(),
        pedido.HistorialEstados.Select(h => new PedidoEstadoHistorialDto(
            h.EstadoAnterior, h.EstadoNuevo, h.Observacion, h.Usuario?.Nombre, h.CreatedAt)).ToList());
}
