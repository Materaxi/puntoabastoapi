using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Inventario;

public class ActualizarUmbralesRequestDto
{
    [Range(0, double.MaxValue)]
    public decimal StockMinimo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? StockMaximo { get; set; }
}
