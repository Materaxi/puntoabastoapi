namespace PuntoAbasto.Api.DTOs.Reportes;

public record MovimientoHistorialDto(
    Guid Id,
    string ProductoNombre,
    string UnidadLabel,
    string Tipo,
    decimal Cantidad,
    decimal StockAnterior,
    decimal StockNuevo,
    string? Motivo,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
