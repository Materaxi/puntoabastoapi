namespace PuntoAbasto.Api.DTOs;

public record HealthResponseDto(string Status, DateTimeOffset Timestamp, string Version);
