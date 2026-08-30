namespace PuntoAbasto.Api.DTOs.Usuarios;

public record CrearUsuarioRequestDto(
    string Nombre,
    string Email,
    string Password,
    string Rol);
