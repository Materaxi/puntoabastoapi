using System.Text.Json.Serialization;

namespace PuntoAbasto.Api.DTOs.Supabase;

/// <summary>Forma cruda (snake_case) de la respuesta de /auth/v1/token de Supabase Auth.</summary>
internal class SupabaseTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>Forma cruda del cuerpo de error que devuelve Supabase Auth (varía según el endpoint).</summary>
internal class SupabaseErrorResponse
{
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonIgnore]
    public string? Message => ErrorDescription ?? Msg;
}
