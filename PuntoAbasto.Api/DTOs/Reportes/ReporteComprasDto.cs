using PuntoAbasto.Api.DTOs.Pedidos;

namespace PuntoAbasto.Api.DTOs.Reportes;

/// <summary>Lista de compra unificada a partir de los pedidos aún no despachados
/// (recibido | confirmado | preparando), para ir a comprar los productos que faltan.</summary>
public record ReporteComprasDto(
    IReadOnlyList<ItemCompraDto> Items,
    IReadOnlyList<PedidoListItemDto> Pedidos);
