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

    [JsonPropertyName("message")]
    public string? MessageField { get; set; }

    [JsonIgnore]
    public string? Message => ErrorDescription ?? Msg ?? MessageField;
}

/// <summary>Body para POST /auth/v1/admin/users (Supabase Admin API — crea un usuario ya confirmado).</summary>
internal class SupabaseAdminCreateUserRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("email_confirm")]
    public bool EmailConfirm { get; set; } = true;

    [JsonPropertyName("user_metadata")]
    public Dictionary<string, string> UserMetadata { get; set; } = new();
}

/// <summary>Body para PUT /auth/v1/admin/users/{id}. "ban_duration" es lo que efectivamente
/// bloquea el login cuando se desactiva un usuario (no alcanza con activo=false local).</summary>
internal class SupabaseAdminUpdateUserRequest
{
    [JsonPropertyName("user_metadata")]
    public Dictionary<string, string>? UserMetadata { get; set; }

    [JsonPropertyName("ban_duration")]
    public string? BanDuration { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

/// <summary>Forma cruda (parcial) de la respuesta de la Admin API al crear/actualizar un usuario.</summary>
internal class SupabaseAdminUserResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
