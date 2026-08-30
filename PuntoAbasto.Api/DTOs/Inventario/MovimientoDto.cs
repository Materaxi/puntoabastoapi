namespace PuntoAbasto.Api.DTOs.Inventario;

public record MovimientoDto(
    Guid Id,
    string Tipo,
    decimal Cantidad,
    decimal StockAnterior,
    decimal StockNuevo,
    string? Motivo,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
