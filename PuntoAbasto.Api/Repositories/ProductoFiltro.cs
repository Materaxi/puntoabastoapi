namespace PuntoAbasto.Api.Repositories;

public record ProductoFiltro(
    int? CategoriaId,
    bool? Activo,
    string? Busqueda,
    int Page,
    int PageSize);
