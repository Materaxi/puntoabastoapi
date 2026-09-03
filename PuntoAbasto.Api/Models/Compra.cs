namespace PuntoAbasto.Api.Models;

/// <summary>Registro de una compra de stock a proveedor, para el costeo de
/// utilidades (cuánto se gasta comprando vs. cuánto se vende). Cada compra
/// también genera un movimiento "entrada" en INVENTARIO_MOVIMIENTOS (ver
/// InventarioMovimiento.CompraId) para que el stock quede consistente.</summary>
public class Compra
{
    public Guid Id { get; set; }
    public Guid ProductoUnidadId { get; set; }
    public Guid? UsuarioId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal { get; set; }
    public string? Proveedor { get; set; }
    public string? Notas { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ProductoUnidad? ProductoUnidad { get; set; }
    public Usuario? Usuario { get; set; }
}
