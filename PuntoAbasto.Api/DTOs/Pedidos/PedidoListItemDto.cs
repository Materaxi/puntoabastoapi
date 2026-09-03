namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoListItemDto(
    Guid Id,
    string Numero,
    string ClienteNombre,
    string ClienteTelefono,
    string ClienteDireccion,
    string Estado,
    string Origen,
    decimal Total,
    bool Pagado,
    string? MetodoPago,
    bool Facturado,
    DateTimeOffset FechaPedido);
