namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoPagoHistorialDto(
    bool Pagado,
    string? MetodoPago,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
