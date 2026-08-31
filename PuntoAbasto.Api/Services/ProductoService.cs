using Microsoft.EntityFrameworkCore;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs.Catalogo;
using PuntoAbasto.Api.DTOs.Common;
using PuntoAbasto.Api.Models;
using PuntoAbasto.Api.Repositories;

namespace PuntoAbasto.Api.Services;

public class ProductoService : IProductoService
{
    private const int PageSizeMaximo = 100;

    private readonly AppDbContext _db;
    private readonly IProductoRepository _productoRepository;

    public ProductoService(AppDbContext db, IProductoRepository productoRepository)
    {
        _db = db;
        _productoRepository = productoRepository;
    }

    public async Task<IReadOnlyList<ProductoPublicoDto>> ListarPublicoAsync(int? categoriaId, CancellationToken ct)
    {
        var productos = await _productoRepository.ListarPublicoAsync(categoriaId, ct);
        return productos.Select(MapToPublicoDto).ToList();
    }

    public async Task<ProductoPublicoDto> ObtenerPublicoPorIdAsync(Guid id, CancellationToken ct)
    {
        var producto = await _productoRepository.ObtenerPublicoPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el producto {id}.");
        return MapToPublicoDto(producto);
    }

    public async Task<PagedResultDto<ProductoInternoDto>> BuscarAsync(
        int? categoriaId, bool? activo, bool? stockBajo, string? busqueda, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new ProductoFiltro(categoriaId, activo, stockBajo, busqueda, page, pageSize);
        var (productos, totalCount) = await _productoRepository.BuscarAsync(filtro, ct);

        return new PagedResultDto<ProductoInternoDto>(productos.Select(MapToInternoDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<ProductoInternoDto> ObtenerInternoPorIdAsync(Guid id, CancellationToken ct)
    {
        var producto = await _productoRepository.ObtenerConDetalleAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el producto {id}.");
        return MapToInternoDto(producto);
    }

    public async Task<ProductoInternoDto> CrearAsync(CrearProductoRequestDto request, CancellationToken ct)
    {
        var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId, ct);
        if (!categoriaExiste)
        {
            throw new ArgumentException($"No existe la categoría {request.CategoriaId}.");
        }

        ValidarUnaSolaDefault(request.Unidades);

        var producto = new Producto
        {
            CategoriaId = request.CategoriaId,
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Emoji = request.Emoji,
            ImagenUrl = request.ImagenUrl,
            Badge = request.Badge,
            Orden = request.Orden,
            Unidades = request.Unidades.Select(u => new ProductoUnidad
            {
                Label = u.Label,
                Precio = u.Precio,
                EsDefault = u.EsDefault,
                Orden = u.Orden,
                Inventario = new Inventario
                {
                    StockActual = u.StockInicial,
                    StockMinimo = u.StockMinimo,
                    StockMaximo = u.StockMaximo,
                    UnidadMedida = u.UnidadMedida
                }
            }).ToList()
        };

        await _productoRepository.AgregarAsync(producto, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerInternoPorIdAsync(producto.Id, ct);
    }

    public async Task<ProductoInternoDto> ActualizarAsync(Guid id, ActualizarProductoRequestDto request, CancellationToken ct)
    {
        var producto = await _productoRepository.ObtenerConDetalleAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el producto {id}.");

        var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId, ct);
        if (!categoriaExiste)
        {
            throw new ArgumentException($"No existe la categoría {request.CategoriaId}.");
        }

        producto.CategoriaId = request.CategoriaId;
        producto.Nombre = request.Nombre;
        producto.Descripcion = request.Descripcion;
        producto.Emoji = request.Emoji;
        producto.ImagenUrl = request.ImagenUrl;
        producto.Badge = request.Badge;
        producto.Activo = request.Activo;
        producto.Disponible = request.Disponible;
        producto.Orden = request.Orden;

        await _db.SaveChangesAsync(ct);

        return MapToInternoDto(producto);
    }

    public async Task<ProductoInternoDto> ActualizarDisponibilidadAsync(Guid id, bool disponible, CancellationToken ct)
    {
        var producto = await _productoRepository.ObtenerConDetalleAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el producto {id}.");

        producto.Disponible = disponible;
        await _db.SaveChangesAsync(ct);

        return MapToInternoDto(producto);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el producto {id}.");

        // Soft delete: un DELETE físico cascadearía producto_unidades → inventario
        // → inventario_movimientos y se perdería el historial de stock, además de
        // dejar huérfanas las referencias de pedidos ya entregados.
        producto.Activo = false;
        producto.Disponible = false;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductoUnidadInternaDto> AgregarUnidadAsync(
        Guid productoId, CrearProductoUnidadRequestDto request, CancellationToken ct)
    {
        var productoExiste = await _db.Productos.AnyAsync(p => p.Id == productoId, ct);
        if (!productoExiste)
        {
            throw new KeyNotFoundException($"No existe el producto {productoId}.");
        }

        var tieneUnidades = await _db.ProductoUnidades.AnyAsync(u => u.ProductoId == productoId, ct);
        var esDefault = request.EsDefault || !tieneUnidades;

        if (esDefault)
        {
            await DesmarcarDefaultActualAsync(productoId, ct);
        }

        var unidad = new ProductoUnidad
        {
            ProductoId = productoId,
            Label = request.Label,
            Precio = request.Precio,
            EsDefault = esDefault,
            Orden = request.Orden,
            Inventario = new Inventario
            {
                StockActual = request.StockInicial,
                StockMinimo = request.StockMinimo,
                StockMaximo = request.StockMaximo,
                UnidadMedida = request.UnidadMedida
            }
        };

        _db.ProductoUnidades.Add(unidad);
        await _db.SaveChangesAsync(ct);

        return MapToUnidadDto(unidad);
    }

    public async Task<ProductoUnidadInternaDto> ActualizarUnidadAsync(
        Guid productoId, Guid unidadId, ActualizarUnidadRequestDto request, CancellationToken ct)
    {
        var unidad = await _db.ProductoUnidades
            .Include(u => u.Inventario)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ProductoId == productoId, ct)
            ?? throw new KeyNotFoundException($"No existe la unidad {unidadId} para el producto {productoId}.");

        ProductoUnidad? promoverADefault = null;

        if (!request.EsDefault && unidad.EsDefault)
        {
            // No puede quedar el producto sin ninguna unidad default: promovemos otra.
            promoverADefault = await _db.ProductoUnidades
                .Where(u => u.ProductoId == productoId && u.Id != unidadId)
                .OrderBy(u => u.Orden)
                .FirstOrDefaultAsync(ct)
                ?? throw new ArgumentException(
                    "El producto debe tener siempre una unidad por defecto; " +
                    "agregá otra unidad antes de quitarle el default a esta.");
        }
        else if (request.EsDefault && !unidad.EsDefault)
        {
            await DesmarcarDefaultActualAsync(productoId, ct);
        }

        unidad.Label = request.Label;
        unidad.Precio = request.Precio;
        unidad.EsDefault = request.EsDefault;
        unidad.Orden = request.Orden;
        await _db.SaveChangesAsync(ct);

        if (promoverADefault is not null)
        {
            promoverADefault.EsDefault = true;
            await _db.SaveChangesAsync(ct);
        }

        return MapToUnidadDto(unidad);
    }

    public async Task EliminarUnidadAsync(Guid productoId, Guid unidadId, CancellationToken ct)
    {
        var unidad = await _db.ProductoUnidades
            .Include(u => u.Inventario)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ProductoId == productoId, ct)
            ?? throw new KeyNotFoundException($"No existe la unidad {unidadId} para el producto {productoId}.");

        var tienePedidos = await _db.PedidoItems.AnyAsync(i => i.ProductoUnidadId == unidadId, ct);
        var tieneMovimientos = unidad.Inventario is not null &&
            await _db.InventarioMovimientos.AnyAsync(m => m.InventarioId == unidad.Inventario.Id, ct);

        if (tienePedidos || tieneMovimientos)
        {
            throw new InvalidOperationException(
                "No se puede eliminar esta unidad: tiene pedidos o movimientos de inventario asociados. " +
                "Marcá el producto como no disponible en vez de borrar la unidad.");
        }

        if (unidad.EsDefault)
        {
            var hayOtras = await _db.ProductoUnidades.AnyAsync(u => u.ProductoId == productoId && u.Id != unidadId, ct);
            if (hayOtras)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar la unidad por defecto mientras existan otras unidades. " +
                    "Marcá otra unidad como default primero.");
            }
        }

        // Cascada borra también la fila de INVENTARIO (sin movimientos, por el chequeo de arriba).
        _db.ProductoUnidades.Remove(unidad);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deja sin default a la(s) unidad(es) que lo tengan, en un SaveChanges aparte
    /// para nunca coexistir dos "true" con la nueva antes de que se confirme la transacción
    /// (el índice único parcial ux_producto_unidades_default lo rechazaría).</summary>
    private async Task DesmarcarDefaultActualAsync(Guid productoId, CancellationToken ct)
    {
        var actuales = await _db.ProductoUnidades
            .Where(u => u.ProductoId == productoId && u.EsDefault)
            .ToListAsync(ct);

        if (actuales.Count == 0)
        {
            return;
        }

        foreach (var unidad in actuales)
        {
            unidad.EsDefault = false;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidarUnaSolaDefault(List<CrearProductoUnidadRequestDto> unidades)
    {
        var defaults = unidades.Count(u => u.EsDefault);
        if (defaults > 1)
        {
            throw new ArgumentException("Solo una unidad puede ser la unidad por defecto.");
        }

        if (defaults == 0)
        {
            unidades[0].EsDefault = true;
        }
    }

    private static ProductoPublicoDto MapToPublicoDto(Producto producto) => new(
        producto.Id,
        producto.Categoria!.Nombre,
        producto.Categoria.Slug,
        producto.Nombre,
        producto.Descripcion,
        producto.Emoji,
        producto.ImagenUrl,
        producto.Badge,
        producto.Disponible,
        producto.Unidades
            .OrderBy(u => u.Orden)
            .Select(u => new ProductoUnidadPublicaDto(u.Id, u.Label, u.Precio, u.EsDefault, u.Inventario?.StockActual > 0))
            .ToList());

    private static ProductoInternoDto MapToInternoDto(Producto producto) => new(
        producto.Id,
        producto.CategoriaId,
        producto.Categoria!.Nombre,
        producto.Nombre,
        producto.Descripcion,
        producto.Emoji,
        producto.ImagenUrl,
        producto.Badge,
        producto.Activo,
        producto.Disponible,
        producto.Orden,
        producto.CreatedAt,
        producto.UpdatedAt,
        producto.Unidades.OrderBy(u => u.Orden).Select(MapToUnidadDto).ToList());

    private static ProductoUnidadInternaDto MapToUnidadDto(ProductoUnidad unidad)
    {
        var inventario = unidad.Inventario!;
        return new ProductoUnidadInternaDto(
            unidad.Id, unidad.Label, unidad.Precio, unidad.EsDefault, unidad.Orden,
            inventario.Id, inventario.StockActual, inventario.StockMinimo, inventario.StockMaximo,
            inventario.UnidadMedida, inventario.AlertaActiva);
    }
}
