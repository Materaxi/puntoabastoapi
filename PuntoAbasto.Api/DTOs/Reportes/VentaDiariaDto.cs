namespace PuntoAbasto.Api.DTOs.Reportes;

public record VentaDiariaDto(DateOnly Fecha, int CantidadPedidos, decimal Total);
