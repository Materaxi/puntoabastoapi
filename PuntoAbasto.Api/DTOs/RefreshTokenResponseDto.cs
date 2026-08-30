namespace PuntoAbasto.Api.DTOs;

public record RefreshTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
