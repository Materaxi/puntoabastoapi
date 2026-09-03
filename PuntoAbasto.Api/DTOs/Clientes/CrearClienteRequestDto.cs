namespace PuntoAbasto.Api.DTOs.Clientes;

public record CrearClienteRequestDto(
    string Nombre,
    string Telefono,
    string Direccion,
    string? ReferenciaDireccion,
    string? UbicacionGps,
    string? Email);
