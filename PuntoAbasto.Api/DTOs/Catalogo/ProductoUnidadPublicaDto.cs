namespace PuntoAbasto.Api.DTOs.Catalogo;

/// <summary>Vista pública de una unidad: precio y si hay stock, sin cantidades exactas.</summary>
public record ProductoUnidadPublicaDto(Guid Id, string Label, decimal Precio, bool EsDefault, bool EnStock);
