using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Clientes;

public class HabilitarAccesoPortalRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string PasswordTemporal { get; set; } = string.Empty;
}
