namespace PuntoAbasto.Api.Models;

/// <summary>
/// usuario_id es nullable: los movimientos automáticos (descuento al
/// confirmar un pedido, reversión al cancelar) los dispara un trigger
/// en Postgres y pueden no traer un usuario humano asociado.
/// </summary>
public class InventarioMovimiento
{
    public Guid Id { get; set; }
    public Guid InventarioId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? UsuarioId { get; set; }

    /// <summary>entrada | salida | ajuste</summary>
    public string Tipo { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockNuevo { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Inventario? Inventario { get; set; }
    public Pedido? Pedido { get; set; }
    public Usuario? Usuario { get; set; }
}
