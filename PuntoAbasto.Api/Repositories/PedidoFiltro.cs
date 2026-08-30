namespace PuntoAbasto.Api.Repositories;

public record PedidoFiltro(
    string? Estado,
    Guid? ClienteId,
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta,
    int Page,
    int PageSize);
