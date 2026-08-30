namespace PuntoAbasto.Api.Models;

public class Config
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public string? Descripcion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
