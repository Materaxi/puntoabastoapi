namespace PuntoAbasto.Api.DTOs.Pedidos;

public record ClienteResumenDto(
    Guid Id,
    string Nombre,
    string Telefono,
    string Direccion,
    string? ReferenciaDireccion,
    string? UbicacionGps,
    string? Email);
