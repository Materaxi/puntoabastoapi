namespace PuntoAbasto.Api.Repositories;

public record InventarioFiltro(bool? AlertaActiva, Guid? ProductoId, string? Q, int Page, int PageSize);
