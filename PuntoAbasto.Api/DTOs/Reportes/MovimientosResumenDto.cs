namespace PuntoAbasto.Api.DTOs.Reportes;

/// <summary>
/// TotalReversionesPorCancelacion son los "ajuste" que genera fn_revertir_stock
/// al cancelar un pedido confirmado (pedido_id NOT NULL) — no son mermas, es
/// stock que vuelve. TotalAjustesManuales y TopMermas solo cuentan ajustes
/// sin pedido_id (los que carga alguien a mano desde /api/inventario/{id}/movimientos).
/// </summary>
public record MovimientosResumenDto(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalEntradas,
    decimal TotalSalidas,
    decimal TotalAjustesManuales,
    decimal TotalReversionesPorCancelacion,
    IReadOnlyList<MermaProductoDto> TopMermas);
