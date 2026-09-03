namespace PuntoAbasto.Api.Helpers;

/// <summary>Métodos de pago válidos para PEDIDOS.metodo_pago, mismo patrón que
/// PedidoEstadoTransiciones.EstadosValidos.</summary>
public static class MetodosPago
{
    public static readonly IReadOnlySet<string> Validos = new HashSet<string>
    {
        "qr", "efectivo", "transferencia"
    };
}
