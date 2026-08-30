namespace PuntoAbasto.Api.Options;

public class AppOptions
{
    public const string SectionName = "App";

    public string FrontendUrl { get; set; } = "https://puntoabasto.com.bo";
    public string Moneda { get; set; } = "Bs";
    public string NegocioNombre { get; set; } = "Punto Abasto";
    public string WhatsappNumero { get; set; } = string.Empty;
}
