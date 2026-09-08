using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Usuarios;

public class RestablecerPasswordUsuarioRequestDto
{
    [Required, StringLength(100, MinimumLength = 8)]
    public string PasswordTemporal { get; set; } = string.Empty;
}
