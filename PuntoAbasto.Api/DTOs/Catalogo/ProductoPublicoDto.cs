namespace PuntoAbasto.Api.DTOs.Catalogo;

public record ProductoPublicoDto(
    Guid Id,
    string CategoriaNombre,
    string CategoriaSlug,
    string Nombre,
    string? Descripcion,
    string? Emoji,
    string? ImagenUrl,
    string? Badge,
    bool Disponible,
    IReadOnlyList<ProductoUnidadPublicaDto> Unidades);
