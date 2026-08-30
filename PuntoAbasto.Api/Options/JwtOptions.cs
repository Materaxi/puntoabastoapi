namespace PuntoAbasto.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
