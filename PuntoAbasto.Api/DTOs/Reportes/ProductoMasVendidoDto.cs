namespace PuntoAbasto.Api.DTOs.Reportes;

public record ProductoMasVendidoDto(string ProductoNombre, string UnidadLabel, decimal CantidadVendida, decimal TotalIngresos);
