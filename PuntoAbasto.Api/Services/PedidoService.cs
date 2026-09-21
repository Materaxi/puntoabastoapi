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
        // Dirección/referencia/email son opcionales (el formulario corto del
        // storefront solo pide nombre+teléfono; el resto se completa después
        // por WhatsApp, ver ClienteFormModal): si el pedido no trae un dato,
        // NO pisamos lo que ya había en el cliente con un valor vacío.
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == request.ClienteTelefono, ct);
        if (cliente is null)
        {
            cliente = new Cliente
            {
                Nombre = request.ClienteNombre,
                Telefono = request.ClienteTelefono,
                Direccion = request.ClienteDireccion ?? string.Empty,
                ReferenciaDireccion = request.ClienteReferenciaDireccion,
                Email = request.ClienteEmail
            };
            _db.Clientes.Add(cliente);
        }
        else
        {
            cliente.Nombre = request.ClienteNombre;
            if (!string.IsNullOrWhiteSpace(request.ClienteDireccion))
            {
                cliente.Direccion = request.ClienteDireccion;
            }
            if (!string.IsNullOrWhiteSpace(request.ClienteReferenciaDireccion))
            {
                cliente.ReferenciaDireccion = request.ClienteReferenciaDireccion;
            }
            if (!string.IsNullOrWhiteSpace(request.ClienteEmail))
            {
                cliente.Email = request.ClienteEmail;
            }
        }

        var items = await ConstruirItemsAsync(request.Items, ct);

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
            Total = PedidoTotales.Calcular(subtotalPedido, request.Descuento, facturado: false),
            FormaPago = request.FormaPago,
            Notas = request.Notas,
            FechaEntregaEst = DateTimeOffset.UtcNow.AddHours(24),
            Items = items
        };

        await _pedidoRepository.AgregarAsync(pedido, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedido.Id, ct);
    }

    public async Task<PedidoDetalleDto> CrearParaClienteAsync(Guid clienteId, CrearPedidoPortalRequestDto request, CancellationToken ct)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct)
            ?? throw new KeyNotFoundException($"No existe el cliente {clienteId}.");

        var items = await ConstruirItemsAsync(request.Items, ct);
        var subtotalPedido = items.Sum(i => i.Subtotal);

        var pedido = new Pedido
        {
            Cliente = cliente,
            Estado = "recibido",
            Origen = "portal",
            Subtotal = subtotalPedido,
            Descuento = 0,
            Total = PedidoTotales.Calcular(subtotalPedido, descuento: 0, facturado: false),
            FormaPago = request.FormaPago,
            Notas = request.Notas,
            FechaEntregaEst = DateTimeOffset.UtcNow.AddHours(24),
            Items = items
        };

        await _pedidoRepository.AgregarAsync(pedido, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedido.Id, ct);
    }

    /// <summary>Valida existencia/disponibilidad de las unidades pedidas y arma los
    /// PedidoItem con precio/subtotal congelados al momento del pedido. Compartido
    /// por CrearAsync (carrito público) y CrearParaClienteAsync (portal B2B).</summary>
    private async Task<List<PedidoItem>> ConstruirItemsAsync(List<CrearPedidoItemDto> itemsRequest, CancellationToken ct)
    {
        var unidadIds = itemsRequest.Select(i => i.ProductoUnidadId).Distinct().ToList();
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
        foreach (var itemRequest in itemsRequest)
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

        return items;
    }

    public async Task<PedidoDetalleDto> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");

        return MapToDetalleDto(pedido);
    }

    public async Task<PagedResultDto<PedidoListItemDto>> BuscarAsync(
        IReadOnlyList<string>? estados, Guid? clienteId, bool? pagado, DateTimeOffset? desde, DateTimeOffset? hasta,
        bool excluirEntregadosPagados, int page, int pageSize, CancellationToken ct)
    {
        var estadoInvalido = estados?.FirstOrDefault(e => !PedidoEstadoTransiciones.EstadosValidos.Contains(e));
        if (!string.IsNullOrEmpty(estadoInvalido))
        {
            throw new ArgumentException($"Estado '{estadoInvalido}' no es válido.");
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > PageSizeMaximo ? 20 : pageSize;

        var filtro = new PedidoFiltro(estados, clienteId, pagado, desde, hasta, excluirEntregadosPagados, page, pageSize);
        var (pedidos, totalCount) = await _pedidoRepository.BuscarAsync(filtro, ct);

        // Trae la última observación de entrega (marcada "no conforme") de cada
        // pedido de esta página, en una sola query aparte: PEDIDO_ESTADOS no forma
        // parte del listado paginado (traerlo con Include multiplicaría filas por
        // Skip/Take), pero acá el universo ya está acotado a pageSize. Ojo: se
        // busca por TODOS los pedidos de la página, no solo los que hoy están
        // "entregado" — un pedido que se entregó no conforme y después se canceló
        // (ver PED-0031) sigue teniendo esa observación en su historial aunque su
        // estado actual ya no sea "entregado".
        var idsPagina = pedidos.Select(p => p.Id).ToList();
        var observaciones = idsPagina.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.PedidoEstados
                .Where(pe => idsPagina.Contains(pe.PedidoId) && pe.EstadoNuevo == "entregado" && pe.Observacion != null)
                .GroupBy(pe => pe.PedidoId)
                .Select(g => g.OrderByDescending(pe => pe.CreatedAt).First())
                .ToDictionaryAsync(pe => pe.PedidoId, pe => pe.Observacion!, ct);

        var items = pedidos.Select(p => MapToListItemDto(p, observaciones.GetValueOrDefault(p.Id))).ToList();
        return new PagedResultDto<PedidoListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<PedidoDetalleDto> ActualizarPagoAsync(
        Guid id, bool pagado, string? metodoPago, ClaimsPrincipal usuario, CancellationToken ct)
    {
        if (pagado)
        {
            if (string.IsNullOrWhiteSpace(metodoPago) || !MetodosPago.Validos.Contains(metodoPago))
            {
                throw new ArgumentException(
                    $"metodoPago debe ser uno de: {string.Join(", ", MetodosPago.Validos)}.");
            }
        }
        else
        {
            metodoPago = null;
        }

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");

        pedido.Pagado = pagado;
        pedido.MetodoPago = metodoPago;
        pedido.FechaPago = pagado ? DateTimeOffset.UtcNow : null;
        pedido.HistorialPago.Add(new PedidoPagoHistorial
        {
            PedidoId = pedido.Id,
            UsuarioId = usuario.GetUsuarioId(),
            Pagado = pagado,
            MetodoPago = metodoPago
        });

        await _db.SaveChangesAsync(ct);

        // Recargamos el pedido completo (no MapToDetalleDto directo) para que
        // HistorialPago.Usuario venga poblado en la respuesta: la fila recién
        // agregada solo tiene UsuarioId seteado, no la navegación cargada.
        return await ObtenerPorIdAsync(id, ct);
    }

    public async Task<PedidoDetalleDto> ActualizarFacturadoAsync(Guid id, bool facturado, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");

        if (pedido.Estado == "cancelado")
        {
            throw new InvalidOperationException("No se puede facturar un pedido cancelado.");
        }

        if (pedido.Pagado)
        {
            throw new InvalidOperationException(
                "No se puede cambiar la facturación de un pedido ya pagado. Desmarcá el pago primero si necesitás corregirlo.");
        }

        var totalAnterior = pedido.Total;
        pedido.Facturado = facturado;
        pedido.Total = PedidoTotales.Calcular(pedido.Subtotal, pedido.Descuento, facturado);

        // Mismo motivo que en ActualizarPrecioItemAsync: si el pedido ya está
        // entregado, trg_actualizar_totales_cliente ya sumó el total original.
        if (pedido.Estado == "entregado" && pedido.Cliente is not null)
        {
            pedido.Cliente.TotalGastado += pedido.Total - totalAnterior;
        }

        await _db.SaveChangesAsync(ct);

        return MapToDetalleDto(pedido);
    }

    public async Task<PedidoDetalleDto> ActualizarPrecioItemAsync(
        Guid pedidoId, Guid itemId, decimal nuevoPrecio, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {pedidoId}.");

        ValidarPedidoEditable(pedido);

        var item = pedido.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"El pedido {pedido.Numero} no tiene el ítem {itemId}.");

        var totalAnterior = pedido.Total;
        var precioAnterior = item.PrecioUnit;

        item.PrecioUnit = nuevoPrecio;
        item.Subtotal = nuevoPrecio * item.Cantidad;
        item.HistorialPrecios.Add(new PedidoItemPrecioHistorial
        {
            PedidoItemId = item.Id,
            UsuarioId = usuario.GetUsuarioId(),
            PrecioAnterior = precioAnterior,
            PrecioNuevo = nuevoPrecio
        });

        RecalcularTotales(pedido, totalAnterior);

        await _db.SaveChangesAsync(ct);

        // Mismo motivo que en ActualizarPagoAsync: recargar para que
        // HistorialPrecios.Usuario venga poblado en la respuesta.
        return await ObtenerPorIdAsync(pedidoId, ct);
    }

    public async Task<PedidoDetalleDto> AgregarItemAsync(
        Guid pedidoId, Guid productoUnidadId, decimal cantidad, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {pedidoId}.");

        ValidarPedidoEditable(pedido);

        var unidad = await _db.ProductoUnidades
            .Include(u => u.Producto)
            .FirstOrDefaultAsync(u => u.Id == productoUnidadId, ct)
            ?? throw new ArgumentException($"No existe la unidad de producto {productoUnidadId}.");

        var producto = unidad.Producto!;
        if (!producto.Activo || !producto.Disponible)
        {
            throw new InvalidOperationException($"'{producto.Nombre}' no está disponible actualmente.");
        }

        var totalAnterior = pedido.Total;
        var itemExistente = pedido.Items.FirstOrDefault(i => i.ProductoUnidadId == productoUnidadId);

        if (itemExistente is not null)
        {
            itemExistente.Cantidad += cantidad;
            itemExistente.Subtotal = itemExistente.PrecioUnit * itemExistente.Cantidad;
        }
        else
        {
            pedido.Items.Add(new PedidoItem
            {
                PedidoId = pedido.Id,
                ProductoUnidadId = unidad.Id,
                ProductoNombre = producto.Nombre,
                UnidadLabel = unidad.Label,
                PrecioUnit = unidad.Precio,
                Cantidad = cantidad,
                Subtotal = unidad.Precio * cantidad
            });
        }

        // El stock recién se descuenta al confirmar (trg_descontar_stock lee
        // PEDIDO_ITEMS en ese momento); si el pedido ya pasó de "recibido" ese
        // descuento ya ocurrió con lo que había entonces, así que lo que se
        // agrega ahora hay que descontarlo a mano.
        if (pedido.Estado != "recibido")
        {
            await AjustarStockPorEdicionItemAsync(
                productoUnidadId, -cantidad,
                $"Ítem agregado al pedido {pedido.Numero} después de confirmado", usuario, ct);
        }

        RecalcularTotales(pedido, totalAnterior);

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedidoId, ct);
    }

    public async Task<PedidoDetalleDto> ActualizarCantidadItemAsync(
        Guid pedidoId, Guid itemId, decimal nuevaCantidad, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {pedidoId}.");

        ValidarPedidoEditable(pedido);

        var item = pedido.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"El pedido {pedido.Numero} no tiene el ítem {itemId}.");

        var totalAnterior = pedido.Total;
        var delta = nuevaCantidad - item.Cantidad;

        item.Cantidad = nuevaCantidad;
        item.Subtotal = item.PrecioUnit * nuevaCantidad;

        if (pedido.Estado != "recibido" && item.ProductoUnidadId is Guid productoUnidadId)
        {
            await AjustarStockPorEdicionItemAsync(
                productoUnidadId, -delta,
                $"Ajuste de cantidad en pedido {pedido.Numero}", usuario, ct);
        }

        RecalcularTotales(pedido, totalAnterior);

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedidoId, ct);
    }

    public async Task<PedidoDetalleDto> EliminarItemAsync(
        Guid pedidoId, Guid itemId, ClaimsPrincipal usuario, CancellationToken ct)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct)
            ?? throw new KeyNotFoundException($"No existe el pedido {pedidoId}.");

        ValidarPedidoEditable(pedido);

        var item = pedido.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"El pedido {pedido.Numero} no tiene el ítem {itemId}.");

        if (pedido.Items.Count == 1)
        {
            throw new InvalidOperationException("El pedido debe tener al menos un ítem.");
        }

        var totalAnterior = pedido.Total;

        if (pedido.Estado != "recibido" && item.ProductoUnidadId is Guid productoUnidadId)
        {
            await AjustarStockPorEdicionItemAsync(
                productoUnidadId, item.Cantidad,
                $"Ítem quitado del pedido {pedido.Numero}", usuario, ct);
        }

        pedido.Items.Remove(item);
        _db.PedidoItems.Remove(item);

        RecalcularTotales(pedido, totalAnterior);

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(pedidoId, ct);
    }

    private static void ValidarPedidoEditable(Pedido pedido)
    {
        if (pedido.Estado == "cancelado")
        {
            throw new InvalidOperationException("No se pueden editar los ítems de un pedido cancelado.");
        }

        if (pedido.Pagado)
        {
            throw new InvalidOperationException(
                "No se pueden editar los ítems de un pedido ya pagado. Desmarcá el pago primero si necesitás corregirlo.");
        }
    }

    /// <summary>Recalcula Subtotal/Total tras editar ítems y, si el pedido ya está
    /// entregado, corrige total_gastado del cliente por la diferencia (mismo motivo
    /// que en ActualizarFacturadoAsync: trg_actualizar_totales_cliente ya sumó el
    /// total original al entregar).</summary>
    private static void RecalcularTotales(Pedido pedido, decimal totalAnterior)
    {
        pedido.Subtotal = pedido.Items.Sum(i => i.Subtotal);
        if (pedido.Descuento > pedido.Subtotal)
        {
            throw new ArgumentException(
                $"El cambio deja el descuento (Bs {pedido.Descuento}) por encima del subtotal (Bs {pedido.Subtotal}). Ajustá el descuento primero.");
        }
        pedido.Total = PedidoTotales.Calcular(pedido.Subtotal, pedido.Descuento, pedido.Facturado);

        if (pedido.Estado == "entregado" && pedido.Cliente is not null)
        {
            pedido.Cliente.TotalGastado += pedido.Total - totalAnterior;
        }
    }

    /// <summary>Ajusta INVENTARIO.stock_actual a mano cuando se edita un ítem de un
    /// pedido que ya pasó por "confirmado" (el trigger que descuenta stock solo corre
    /// una vez, al confirmar, con los ítems que había en ese momento). delta positivo
    /// = repone stock (ítem quitado o cantidad reducida), negativo = consume más
    /// (ítem agregado o cantidad aumentada). Deja un movimiento en
    /// INVENTARIO_MOVIMIENTOS para auditoría, igual que InventarioService.</summary>
    private async Task AjustarStockPorEdicionItemAsync(
        Guid productoUnidadId, decimal delta, string motivo, ClaimsPrincipal usuario, CancellationToken ct)
    {
        if (delta == 0)
        {
            return;
        }

        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.ProductoUnidadId == productoUnidadId, ct);
        if (inventario is null)
        {
            // Sin registro de inventario para esta unidad: si hace falta consumir
            // stock (delta negativo) no hay dónde descontarlo; si es una reversión
            // (delta positivo) no hay nada que reponer, se ignora igual que
            // fn_revertir_stock cuando no encuentra la fila.
            if (delta < 0)
            {
                throw new InvalidOperationException("No existe registro de inventario para el producto seleccionado.");
            }
            return;
        }

        var stockAnterior = inventario.StockActual;
        var stockNuevo = stockAnterior + delta;
        inventario.StockActual = stockNuevo;

        _db.InventarioMovimientos.Add(new InventarioMovimiento
        {
            InventarioId = inventario.Id,
            UsuarioId = usuario.GetUsuarioId(),
            Tipo = "ajuste",
            Cantidad = delta,
            StockAnterior = stockAnterior,
            StockNuevo = stockNuevo,
            Motivo = motivo
        });
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

        // Delivery solo entrega: no confirma, prepara, despacha ni cancela pedidos.
        var esDelivery = usuario.HasClaim(SupabaseRoleClaimsTransformation.RoleClaimType, "delivery");
        if (esDelivery && nuevoEstado != "entregado")
        {
            throw new UnauthorizedAccessException("Delivery solo puede marcar pedidos como entregados.");
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

    private static PedidoListItemDto MapToListItemDto(Pedido pedido, string? observacionEntrega = null) => new(
        pedido.Id,
        pedido.Numero,
        pedido.Cliente!.Nombre,
        pedido.Cliente.Telefono,
        pedido.Cliente.Direccion,
        pedido.Estado,
        pedido.Origen,
        pedido.Total,
        pedido.Pagado,
        pedido.MetodoPago,
        pedido.Facturado,
        pedido.FechaPedido,
        observacionEntrega);

    private static PedidoDetalleDto MapToDetalleDto(Pedido pedido) => new(
        pedido.Id,
        pedido.Numero,
        new ClienteResumenDto(
            pedido.Cliente!.Id,
            pedido.Cliente.Nombre,
            pedido.Cliente.Telefono,
            pedido.Cliente.Direccion,
            pedido.Cliente.ReferenciaDireccion,
            pedido.Cliente.UbicacionGps,
            pedido.Cliente.Email),
        pedido.Usuario?.Nombre,
        pedido.Estado,
        pedido.Origen,
        pedido.Subtotal,
        pedido.Descuento,
        pedido.Total,
        pedido.Pagado,
        pedido.MetodoPago,
        pedido.FormaPago,
        pedido.FechaPago,
        pedido.Facturado,
        pedido.Notas,
        pedido.FechaPedido,
        pedido.FechaEntregaEst,
        pedido.FechaEntregaReal,
        pedido.Items.Select(i => new PedidoItemDto(
            i.Id, i.ProductoUnidadId, i.ProductoNombre, i.UnidadLabel, i.PrecioUnit, i.Cantidad, i.Subtotal,
            i.HistorialPrecios.Select(h => new PedidoItemPrecioHistorialDto(
                h.PrecioAnterior, h.PrecioNuevo, h.Usuario?.Nombre, h.CreatedAt)).ToList())).ToList(),
        pedido.HistorialEstados.Select(h => new PedidoEstadoHistorialDto(
            h.EstadoAnterior, h.EstadoNuevo, h.Observacion, h.Usuario?.Nombre, h.CreatedAt)).ToList(),
        pedido.HistorialPago.Select(h => new PedidoPagoHistorialDto(
            h.Pagado, h.MetodoPago, h.Usuario?.Nombre, h.CreatedAt)).ToList());
}
