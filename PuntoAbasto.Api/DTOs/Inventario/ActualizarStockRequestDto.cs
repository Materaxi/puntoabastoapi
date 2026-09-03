namespace PuntoAbasto.Api.DTOs.Inventario;

/// <summary>El conteo total actual del producto en almacén, no un delta.</summary>
public class ActualizarStockRequestDto
{
    public decimal StockActual { get; set; }
}
