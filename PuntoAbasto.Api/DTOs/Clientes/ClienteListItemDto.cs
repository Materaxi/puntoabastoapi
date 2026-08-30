namespace PuntoAbasto.Api.DTOs.Clientes;

public record ClienteListItemDto(
    Guid Id,
    string Nombre,
    string Telefono,
    string Direccion,
    int TotalPedidos,
    decimal TotalGastado,
    DateTimeOffset CreatedAt);
