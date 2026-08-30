namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// Formatea números correlativos con prefijo y padding fijo (PED-0001, NV-0001).
/// La generación del siguiente correlativo vive en Postgres (fn_numero_pedido /
/// fn_numero_nota en schema.sql) para evitar condiciones de carrera entre
/// instancias de la API; este helper solo formatea si se necesita en memoria.
/// </summary>
public static class NumeracionHelper
{
    public static string Formatear(string prefijo, long correlativo, int padding = 4)
        => $"{prefijo}-{correlativo.ToString().PadLeft(padding, '0')}";
}
