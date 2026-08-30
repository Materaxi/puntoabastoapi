namespace PuntoAbasto.Api.Repositories;

public record NotaVentaFiltro(string? Estado, DateTimeOffset? Desde, DateTimeOffset? Hasta, int Page, int PageSize);
