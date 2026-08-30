namespace PuntoAbasto.Api.DTOs;

public record UsuarioDto(
    Guid Id,
    string Nombre,
    string Email,
    string Rol,
    bool Activo,
    DateTimeOffset? UltimoLogin);
