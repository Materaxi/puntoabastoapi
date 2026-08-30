namespace PuntoAbasto.Api.DTOs.Catalogo;

/// <summary>Toggle rápido para que un vendedor marque "agotado" sin abrir el formulario completo.</summary>
public class ActualizarDisponibilidadRequestDto
{
    public bool Disponible { get; set; }
}
