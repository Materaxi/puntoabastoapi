namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoPagoHistorialDto(
    bool Pagado,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
