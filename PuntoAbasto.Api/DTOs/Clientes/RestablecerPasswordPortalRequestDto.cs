using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Clientes;

public class RestablecerPasswordPortalRequestDto
{
    [Required, StringLength(100, MinimumLength = 8)]
    public string PasswordTemporal { get; set; } = string.Empty;
}
