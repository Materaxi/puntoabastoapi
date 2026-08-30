using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
