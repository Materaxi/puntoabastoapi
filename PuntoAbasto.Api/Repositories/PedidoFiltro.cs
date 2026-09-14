namespace PuntoAbasto.Api.Repositories;

public record PedidoFiltro(
    IReadOnlyList<string>? Estados,
    Guid? ClienteId,
    bool? Pagado,
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta,
    /// <summary>Oculta pedidos entregados y ya pagados (nada pendiente por hacer),
    /// sin importar si "entregado" está en <see cref="Estados"/>. Independiente de
    /// Pagado: ese filtro exige/excluye pago en TODOS los estados seleccionados.</summary>
    bool ExcluirEntregadosPagados,
    int Page,
    int PageSize);
