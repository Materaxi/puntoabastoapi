namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoItemPrecioHistorialDto(
    decimal PrecioAnterior,
    decimal PrecioNuevo,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
