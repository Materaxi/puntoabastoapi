namespace PuntoAbasto.Api.Helpers;

/// <summary>
/// Railway inyecta variables de entorno "planas" (DATABASE_URL, SUPABASE_URL, etc.)
/// en vez de la notación anidada con "__" que usa por defecto la configuración de
/// .NET. Este helper las mapea a las claves anidadas que usa el resto de la app
/// (ConnectionStrings:*, Supabase:*, Jwt:*, App:*) sin necesidad de hardcodear
/// ningún secreto en appsettings.json.
/// </summary>
public static class EnvironmentConfig
{
    public static void Apply(IConfigurationBuilder configuration)
    {
        var overrides = new Dictionary<string, string?>();

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            var normalized = ConnectionStringHelper.Normalize(databaseUrl);
            overrides["ConnectionStrings:PooledConnection"] = normalized;
            overrides["ConnectionStrings:DefaultConnection"] = normalized;
        }

        MapIfPresent("SUPABASE_URL", "Supabase:Url", overrides);
        MapIfPresent("SUPABASE_ANON_KEY", "Supabase:AnonKey", overrides);
        MapIfPresent("SUPABASE_SERVICE_KEY", "Supabase:ServiceRoleKey", overrides);
        MapIfPresent("SUPABASE_SERVICE_ROLE_KEY", "Supabase:ServiceRoleKey", overrides);
        MapIfPresent("SUPABASE_PROJECT_REF", "Supabase:ProjectRef", overrides);

        MapIfPresent("JWT_EXPIRY_MINUTES", "Jwt:ExpiryMinutes", overrides);
        MapIfPresent("REFRESH_TOKEN_DAYS", "Jwt:RefreshTokenDays", overrides);

        MapIfPresent("FRONTEND_URL", "App:FrontendUrl", overrides);

        if (overrides.Count > 0)
        {
            configuration.AddInMemoryCollection(overrides);
        }
    }

    private static void MapIfPresent(string envVar, string configKey, Dictionary<string, string?> overrides)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(value))
        {
            overrides[configKey] = value;
        }
    }
}
