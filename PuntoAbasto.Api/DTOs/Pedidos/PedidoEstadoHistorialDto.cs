namespace PuntoAbasto.Api.DTOs.Pedidos;

public record PedidoEstadoHistorialDto(
    string? EstadoAnterior,
    string EstadoNuevo,
    string? Observacion,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
