namespace PuntoAbasto.Api.Repositories;

public record PedidoFiltro(
    string? Estado,
    Guid? ClienteId,
    bool? Pagado,
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta,
    int Page,
    int PageSize);
