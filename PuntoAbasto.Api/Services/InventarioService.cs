using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.DTOs.Inventario;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Models;
using PuntoAbasto.Api.Repositories;

namespace PuntoAbasto.Api.Services;

public class InventarioService : IInventarioService
{
    private static readonly HashSet<string> TiposManualesValidos = ["entrada", "ajuste"];
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;
    private readonly IInventarioRepository _inventarioRepository;

    public InventarioService(AppDbContext db, IInventarioRepository inventarioRepository)
    {
        _db = db;
        _inventarioRepository = inventarioRepository;
    }

    public async Task<PagedResultDto<InventarioDto>> BuscarAsync(
        bool? alertaActiva, Guid? productoId, string? q, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new InventarioFiltro(alertaActiva, productoId, q, page, pageSize);
        var (items, totalCount) = await _inventarioRepository.BuscarAsync(filtro, ct);

        return new PagedResultDto<InventarioDto>(items.Select(MapToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<InventarioDto>> ListarAlertasAsync(CancellationToken ct)
    {
        var items = await _inventarioRepository.ListarAlertasAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<InventarioDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var inventario = await _inventarioRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el registro de inventario {id}.");
        return MapToDto(inventario);
    }

    public async Task<PagedResultDto<MovimientoDto>> ListarMovimientosAsync(Guid id, int page, int pageSize, CancellationToken ct)
    {
        var existe = await _db.Inventarios.AnyAsync(i => i.Id == id, ct);
        if (!existe)
        {
            throw new KeyNotFoundException($"No existe el registro de inventario {id}.");
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var (items, totalCount) = await _inventarioRepository.ListarMovimientosAsync(id, page, pageSize, ct);

        var dtos = items
            .Select(m => new MovimientoDto(m.Id, m.Tipo, m.Cantidad, m.StockAnterior, m.StockNuevo, m.Motivo, m.Usuario?.Nombre, m.CreatedAt))
            .ToList();

        return new PagedResultDto<MovimientoDto>(dtos, page, pageSize, totalCount);
    }

    public async Task<InventarioDto> RegistrarMovimientoAsync(
        Guid id, RegistrarMovimientoRequestDto request, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var tipo = request.Tipo.Trim().ToLowerInvariant();
        if (!TiposManualesValidos.Contains(tipo))
        {
            throw new ArgumentException(
                $"Tipo '{request.Tipo}' no es válido para un movimiento manual. Debe ser 'entrada' o 'ajuste' " +
                "('salida' la genera automáticamente el sistema al confirmar un pedido).");
        }

        if (request.Cantidad == 0)
        {
            throw new ArgumentException("La cantidad del movimiento no puede ser 0.");
        }

        if (tipo == "entrada" && request.Cantidad < 0)
        {
            throw new ArgumentException("Una 'entrada' debe tener cantidad positiva.");
        }

        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el registro de inventario {id}.");

        var stockAnterior = inventario.StockActual;
        var stockNuevo = stockAnterior + request.Cantidad;

        if (stockNuevo < 0)
        {
            throw new ArgumentException($"El ajuste dejaría el stock en {stockNuevo} (negativo). Stock actual: {stockAnterior}.");
        }

        inventario.StockActual = stockNuevo;

        _db.InventarioMovimientos.Add(new InventarioMovimiento
        {
            InventarioId = inventario.Id,
            UsuarioId = usuario.GetUsuarioId(),
            Tipo = tipo,
            Cantidad = request.Cantidad,
            StockAnterior = stockAnterior,
            StockNuevo = stockNuevo,
            Motivo = request.Motivo
        });

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    public async Task<InventarioDto> ActualizarStockAsync(Guid id, decimal stockActual, ClaimsPrincipal usuario, CancellationToken ct)
    {
        if (stockActual < 0)
        {
            throw new ArgumentException("El stock no puede ser negativo.");
        }

        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el registro de inventario {id}.");

        var stockAnterior = inventario.StockActual;
        var delta = stockActual - stockAnterior;

        inventario.StockActual = stockActual;

        // Sin movimiento si el conteo confirma el mismo stock que ya había (nada que auditar).
        if (delta != 0)
        {
            _db.InventarioMovimientos.Add(new InventarioMovimiento
            {
                InventarioId = inventario.Id,
                UsuarioId = usuario.GetUsuarioId(),
                Tipo = "ajuste",
                Cantidad = delta,
                StockAnterior = stockAnterior,
                StockNuevo = stockActual,
                Motivo = "Conteo de stock (almacén)"
            });
        }

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    public async Task<InventarioDto> ActualizarUmbralesAsync(Guid id, ActualizarUmbralesRequestDto request, CancellationToken ct)
    {
        if (request.StockMaximo is not null && request.StockMaximo < request.StockMinimo)
        {
            throw new ArgumentException("stock_maximo no puede ser menor a stock_minimo.");
        }

        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el registro de inventario {id}.");

        inventario.StockMinimo = request.StockMinimo;
        inventario.StockMaximo = request.StockMaximo;

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    private static InventarioDto MapToDto(Inventario inventario)
    {
        var unidad = inventario.ProductoUnidad!;
        var producto = unidad.Producto!;

        return new InventarioDto(
            inventario.Id, unidad.Id, producto.Id, producto.Nombre, unidad.Label,
            inventario.StockActual, inventario.StockMinimo, inventario.StockMaximo,
            inventario.UnidadMedida, inventario.AlertaActiva, inventario.UpdatedAt);
    }
}
