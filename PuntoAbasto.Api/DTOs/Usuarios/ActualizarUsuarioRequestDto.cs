namespace PuntoAbasto.Api.DTOs.Usuarios;

public record ActualizarUsuarioRequestDto(
    string Nombre,
    string Rol,
    bool Activo);
