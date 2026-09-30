namespace PuntoAbasto.Api.DTOs.Catalogo;

/// <summary>Vista pública de una unidad: precio y si hay stock, sin cantidades exactas.
/// UnidadMedida (kg | unidad | arroba | caja | atado | bolsa) le dice al frontend si la
/// cantidad se puede fraccionar: solo "unidad" (ítems que no se venden partidos, ej. un
/// limón) se restringe a incrementos de a 1; el resto sigue permitiendo 0.5.</summary>
public record ProductoUnidadPublicaDto(Guid Id, string Label, decimal Precio, bool EsDefault, bool EnStock, string? UnidadMedida);
