using System.ComponentModel.DataAnnotations;

namespace PuntoAbasto.Api.DTOs.Catalogo;

public class CrearProductoUnidadRequestDto
{
    [Required, StringLength(50)]
    public string Label { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    public bool EsDefault { get; set; }

    public int Orden { get; set; }

    [Range(0, double.MaxValue)]
    public decimal StockInicial { get; set; }

    [Range(0, double.MaxValue)]
    public decimal StockMinimo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? StockMaximo { get; set; }

    [Required, StringLength(30)]
    public string UnidadMedida { get; set; } = string.Empty;
}
