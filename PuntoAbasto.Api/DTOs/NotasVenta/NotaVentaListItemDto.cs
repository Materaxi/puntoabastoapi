namespace PuntoAbasto.Api.DTOs.NotasVenta;

public record NotaVentaListItemDto(
    Guid Id,
    string Numero,
    string PedidoNumero,
    string ClienteNombre,
    decimal Total,
    string Estado,
    DateTimeOffset FechaEmision);
