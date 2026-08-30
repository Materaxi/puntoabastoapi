namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoListItemDto(
    Guid Id,
    string Numero,
    string ClienteNombre,
    string ClienteTelefono,
    string Estado,
    string Origen,
    decimal Total,
    DateTimeOffset FechaPedido);
