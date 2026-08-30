namespace PuntoAbasto.Api.Repositories;

public record InventarioFiltro(bool? AlertaActiva, Guid? ProductoId, int Page, int PageSize);
