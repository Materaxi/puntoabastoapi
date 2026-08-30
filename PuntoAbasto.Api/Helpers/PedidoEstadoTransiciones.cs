namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// Máquina de estados de PEDIDOS.estado. Vive en la API (no solo en el
/// CHECK de la BD) para poder devolver un 409 con un mensaje claro antes
/// de tocar la base, en vez de dejar que una transición inválida llegue
/// hasta Postgres.
/// </summary>
public static class PedidoEstadoTransiciones
{
    public static readonly IReadOnlySet<string> EstadosValidos = new HashSet<string>
    {
        "recibido", "confirmado", "preparando", "en_camino", "entregado", "cancelado"
    };

    private static readonly Dictionary<string, string[]> Permitidas = new()
    {
        ["recibido"] = ["confirmado", "cancelado"],
        ["confirmado"] = ["preparando", "cancelado"],
        ["preparando"] = ["en_camino", "cancelado"],
        ["en_camino"] = ["entregado", "cancelado"],
        ["entregado"] = [],
        ["cancelado"] = [],
    };

    public static bool EsTransicionValida(string estadoActual, string estadoNuevo)
        => Permitidas.TryGetValue(estadoActual, out var siguientes) && siguientes.Contains(estadoNuevo);
}
