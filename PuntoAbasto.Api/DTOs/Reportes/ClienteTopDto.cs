namespace PuntoAbasto.Api.DTOs.Reportes;

public record ClienteTopDto(Guid ClienteId, string Nombre, string Telefono, int CantidadPedidos, decimal TotalGastado);
