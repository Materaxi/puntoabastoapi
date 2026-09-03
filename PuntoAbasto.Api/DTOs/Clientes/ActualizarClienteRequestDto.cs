namespace PuntoAbasto.Api.DTOs.Clientes;

public record ActualizarClienteRequestDto(
    string Nombre,
    string Telefono,
    string Direccion,
    string? ReferenciaDireccion,
    string? UbicacionGps,
    string? Email);
