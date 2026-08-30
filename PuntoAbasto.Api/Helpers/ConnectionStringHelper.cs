using Npgsql;

namespace PuntoAbasto.Api.Helpers;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Convierte una connection string en formato URI de Postgres
    /// (postgresql://user:pass@host:port/database) — como la que entrega
    /// Supabase — a una connection string ADO.NET válida para Npgsql.
    /// Si el valor ya viene en formato keyword=value, se retorna sin cambios.
    /// </summary>
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
            Pooling = true
        };

        return builder.ConnectionString;
    }
}
