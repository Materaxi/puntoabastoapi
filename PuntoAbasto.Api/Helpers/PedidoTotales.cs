namespace PuntoAbasto.Api.Helpers;

/// <summary>Cálculo del total de un pedido a partir de subtotal/descuento/facturado.
/// Centralizado acá porque se recalcula en varios puntos: al crear el pedido, al
/// editar el precio de un ítem y al marcar/desmarcar facturado.</summary>
public static class PedidoTotales
{
    /// <summary>IVA Bolivia: un pedido facturado suma 16% sobre (subtotal - descuento).</summary>
    public const decimal FactorFacturado = 1.16m;

    public static decimal Calcular(decimal subtotal, decimal descuento, bool facturado)
    {
        var baseImponible = subtotal - descuento;
        return facturado ? Math.Round(baseImponible * FactorFacturado, 2) : baseImponible;
    }
}
