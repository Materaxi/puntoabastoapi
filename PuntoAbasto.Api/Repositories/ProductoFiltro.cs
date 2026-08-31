namespace PuntoAbasto.Api.Repositories;

public record ProductoFiltro(
    int? CategoriaId,
    bool? Activo,
    bool? StockBajo,
    string? Busqueda,
    int Page,
    int PageSize);
