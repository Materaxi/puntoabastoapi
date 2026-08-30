namespace PuntoAbasto.Api.DTOs.Catalogo;

public record CategoriaDto(int Id, string Nombre, string Slug, string? Emoji, int Orden);
