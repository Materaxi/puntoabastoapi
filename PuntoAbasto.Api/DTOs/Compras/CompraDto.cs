namespace PuntoAbasto.Api.DTOs.Compras;

public record CompraDto(
    Guid Id,
    Guid ProductoUnidadId,
    string ProductoNombre,
    string UnidadLabel,
    decimal Cantidad,
    decimal CostoUnitario,
    decimal CostoTotal,
    string? Proveedor,
    string? Notas,
    string? UsuarioNombre,
    DateTimeOffset CreatedAt);
