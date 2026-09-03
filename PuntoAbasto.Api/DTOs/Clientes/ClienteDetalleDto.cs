namespace PuntoAbasto.Api.DTOs.Clientes;

public record ClienteDetalleDto(
    Guid Id,
    string Nombre,
    string Telefono,
    string Direccion,
    string? ReferenciaDireccion,
    string? UbicacionGps,
    string? Email,
    int TotalPedidos,
    decimal TotalGastado,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
