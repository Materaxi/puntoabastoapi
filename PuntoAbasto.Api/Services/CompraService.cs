using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Compras;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Services;

public class CompraService : ICompraService
{
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;

    public CompraService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CompraDto> RegistrarAsync(RegistrarCompraRequestDto request, ClaimsPrincipal usuario, CancellationToken ct)
    {
        if (request.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.");
        }

        if (request.CostoUnitario <= 0)
        {
            throw new ArgumentException("El costo unitario debe ser mayor a 0.");
        }

        var productoUnidad = await _db.ProductoUnidades
            .Include(u => u.Producto)
            .FirstOrDefaultAsync(u => u.Id == request.ProductoUnidadId, ct)
            ?? throw new KeyNotFoundException($"No existe la unidad de producto {request.ProductoUnidadId}.");

        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.ProductoUnidadId == request.ProductoUnidadId, ct)
            ?? throw new KeyNotFoundException(
                $"No existe un registro de inventario para {productoUnidad.Producto!.Nombre} ({productoUnidad.Label}).");

        var proveedor = string.IsNullOrWhiteSpace(request.Proveedor) ? null : request.Proveedor.Trim();
        var usuarioId = usuario.GetUsuarioId();

        var compra = new Compra
        {
            ProductoUnidadId = productoUnidad.Id,
            UsuarioId = usuarioId,
            Cantidad = request.Cantidad,
            CostoUnitario = request.CostoUnitario,
            CostoTotal = Math.Round(request.Cantidad * request.CostoUnitario, 2),
            Proveedor = proveedor,
            Notas = request.Notas
        };
        _db.Compras.Add(compra);

        var stockAnterior = inventario.StockActual;
        var stockNuevo = stockAnterior + request.Cantidad;
        inventario.StockActual = stockNuevo;

        // Compra = compra (navegación, no CompraId) porque compra.Id todavía no existe:
        // ambos se insertan en el mismo SaveChangesAsync y EF resuelve el FK después
        // de que Postgres genera el id de compra.
        _db.InventarioMovimientos.Add(new InventarioMovimiento
        {
            InventarioId = inventario.Id,
            Compra = compra,
            UsuarioId = usuarioId,
            Tipo = "entrada",
            Cantidad = request.Cantidad,
            StockAnterior = stockAnterior,
            StockNuevo = stockNuevo,
            Motivo = proveedor is null ? "Compra de stock" : $"Compra a {proveedor}"
        });

        await _db.SaveChangesAsync(ct);

        var usuarioNombre = await _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Nombre).FirstOrDefaultAsync(ct);

        return new CompraDto(
            compra.Id, compra.ProductoUnidadId, productoUnidad.Producto!.Nombre, productoUnidad.Label,
            compra.Cantidad, compra.CostoUnitario, compra.CostoTotal, compra.Proveedor, compra.Notas,
            usuarioNombre, compra.CreatedAt);
    }

    public async Task<PagedResultDto<CompraDto>> BuscarAsync(
        Guid? productoId, DateOnly? desde, DateOnly? hasta, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var query = _db.Compras.AsQueryable();

        if (productoId is not null)
        {
            query = query.Where(c => c.ProductoUnidad!.ProductoId == productoId);
        }

        if (desde is not null)
        {
            var desdeUtc = new DateTimeOffset(desde.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(c => c.CreatedAt >= desdeUtc);
        }

        if (hasta is not null)
        {
            var hastaUtc = new DateTimeOffset(hasta.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(c => c.CreatedAt <= hastaUtc);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompraDto(
                c.Id, c.ProductoUnidadId, c.ProductoUnidad!.Producto!.Nombre, c.ProductoUnidad.Label,
                c.Cantidad, c.CostoUnitario, c.CostoTotal, c.Proveedor, c.Notas,
                c.Usuario != null ? c.Usuario.Nombre : null, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResultDto<CompraDto>(items, page, pageSize, totalCount);
    }
}
