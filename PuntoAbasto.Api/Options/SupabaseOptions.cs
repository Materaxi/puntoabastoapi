namespace PuntoAbasto.Api.Options;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string ProjectRef { get; set; } = string.Empty;

    public string Authority => $"{Url.TrimEnd('/')}/auth/v1";
    public string JwksUri => $"{Authority}/.well-known/jwks.json";
}
