using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.NotasVenta;
using PuntoAbasto.Api.DTOs.Pedidos;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;
using PuntoAbasto.Api.Repositories;

namespace PuntoAbasto.Api.Services;

public class NotaVentaService : INotaVentaService
{
    private static readonly HashSet<string> EstadosPedidoQueNoEmiten = ["recibido", "cancelado"];
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;
    private readonly INotaVentaRepository _notaVentaRepository;

    public NotaVentaService(AppDbContext db, INotaVentaRepository notaVentaRepository)
    {
        _db = db;
        _notaVentaRepository = notaVentaRepository;
    }

    public async Task<PagedResultDto<NotaVentaListItemDto>> BuscarAsync(
        string? estado, DateTimeOffset? desde, DateTimeOffset? hasta, int page, int pageSize, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(estado) && estado is not ("emitida" or "anulada"))
        {
            throw new ArgumentException($"Estado '{estado}' no es válido. Debe ser 'emitida' o 'anulada'.");
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new NotaVentaFiltro(estado, desde, hasta, page, pageSize);
        var (items, totalCount) = await _notaVentaRepository.BuscarAsync(filtro, ct);

        return new PagedResultDto<NotaVentaListItemDto>(items.Select(MapToListItemDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<NotaVentaDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var notaVenta = await _notaVentaRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe la nota de venta {id}.");

        return MapToDetalleDto(notaVenta);
    }

    public async Task<NotaVentaDetalleDto> EmitirAsync(EmitirNotaVentaRequestDto request, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoId, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {request.PedidoId}.");

        if (EstadosPedidoQueNoEmiten.Contains(pedido.Estado))
        {
            throw new InvalidOperationException(
                $"No se puede emitir una nota de venta para el pedido {pedido.Numero}: está en estado " +
                $"'{pedido.Estado}'. Tiene que estar confirmado (o en un estado posterior) y no cancelado.");
        }

        var yaTieneNota = await _db.NotasVenta.AnyAsync(n => n.PedidoId == pedido.Id, ct);
        if (yaTieneNota)
        {
            throw new InvalidOperationException($"El pedido {pedido.Numero} ya tiene una nota de venta emitida.");
        }

        var notaVenta = new NotaVenta
        {
            PedidoId = pedido.Id,
            UsuarioId = usuario.GetUsuarioId(),
            Subtotal = pedido.Subtotal,
            Descuento = pedido.Descuento,
            Total = pedido.Total,
            Estado = "emitida",
            Observaciones = request.Observaciones
        };

        await _notaVentaRepository.AgregarAsync(notaVenta, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(notaVenta.Id, ct);
    }

    public async Task<NotaVentaDetalleDto> AnularAsync(Guid id, string observaciones, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var notaVenta = await _db.NotasVenta.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe la nota de venta {id}.");

        if (notaVenta.Estado == "anulada")
        {
            throw new InvalidOperationException($"La nota de venta {notaVenta.Numero} ya está anulada.");
        }

        // No hay tabla de historial para notas_venta (a diferencia de PEDIDO_ESTADOS):
        // se concatena el motivo de anulación en vez de pisar las observaciones originales.
        notaVenta.Observaciones = string.IsNullOrWhiteSpace(notaVenta.Observaciones)
            ? $"[Anulada] {observaciones}"
            : $"{notaVenta.Observaciones}\n[Anulada] {observaciones}";
        notaVenta.Estado = "anulada";
        notaVenta.UsuarioId = usuario.GetUsuarioId();

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    private static NotaVentaListItemDto MapToListItemDto(NotaVenta notaVenta) => new(
        notaVenta.Id,
        notaVenta.Numero,
        notaVenta.Pedido!.Numero,
        notaVenta.Pedido.Cliente!.Nombre,
        notaVenta.Total,
        notaVenta.Estado,
        notaVenta.FechaEmision);

    private static NotaVentaDetalleDto MapToDetalleDto(NotaVenta notaVenta)
    {
        var pedido = notaVenta.Pedido!;
        var cliente = pedido.Cliente!;

        return new NotaVentaDetalleDto(
            notaVenta.Id,
            notaVenta.Numero,
            pedido.Id,
            pedido.Numero,
            new ClienteResumenDto(cliente.Id, cliente.Nombre, cliente.Telefono, cliente.Direccion, cliente.ReferenciaDireccion, cliente.UbicacionGps, cliente.Email),
            notaVenta.Usuario?.Nombre,
            notaVenta.Subtotal,
            notaVenta.Descuento,
            notaVenta.Total,
            notaVenta.Estado,
            notaVenta.Observaciones,
            notaVenta.FechaEmision,
            notaVenta.CreatedAt,
            pedido.Items.Select(i => new PedidoItemDto(
                i.Id, i.ProductoUnidadId, i.ProductoNombre, i.UnidadLabel, i.PrecioUnit, i.Cantidad, i.Subtotal,
                Array.Empty<PedidoItemPrecioHistorialDto>())).ToList());
    }
}
