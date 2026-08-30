namespace PuntoAbasto.Api.DTOs.Catalogo;

public record ProductoInternoDto(
    Guid Id,
    int CategoriaId,
    string CategoriaNombre,
    string Nombre,
    string? Descripcion,
    string? Emoji,
    string? ImagenUrl,
    string? Badge,
    bool Activo,
    bool Disponible,
    int Orden,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ProductoUnidadInternaDto> Unidades);
