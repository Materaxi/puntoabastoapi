using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PuntoAbasto.Api.Data;
using PuntoAbasto.Api.DTOs;
using PuntoAbasto.Api.Helpers;
using PuntoAbasto.Api.Middleware;
using PuntoAbasto.Api.Options;
using PuntoAbasto.Api.Repositories;
using PuntoAbasto.Api.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Railway inyecta variables de entorno planas (DATABASE_URL, SUPABASE_URL, ...).
    // Las mapeamos a la configuración anidada antes de leer cualquier sección.
    EnvironmentConfig.Apply(builder.Configuration);

    builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));
    builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

    var supabaseOptions = builder.Configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()
        ?? new SupabaseOptions();
    var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>()
        ?? new AppOptions();

    // ── Base de datos ────────────────────────────────────────────────
    // Producción usa el connection string pooled de Supabase (puerto 6543,
    // Transaction mode); desarrollo usa la conexión directa (puerto 5432).
    var connectionStringKey = builder.Environment.IsDevelopment() ? "DefaultConnection" : "PooledConnection";
    var connectionString =
        builder.Configuration.GetConnectionString(connectionStringKey) ??
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "No se encontró una cadena de conexión a la base de datos. " +
            "Configurá DATABASE_URL (Railway) o ConnectionStrings:DefaultConnection (desarrollo).");
    }

    builder.Services.AddDbContext<AppDbContext>(options => options
        .UseNpgsql(ConnectionStringHelper.Normalize(connectionString))
        .UseSnakeCaseNamingConvention());

    // ── Controllers ──────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ── CORS ─────────────────────────────────────────────────────────
    const string corsPolicyName = "PuntoAbastoCors";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicyName, policy => policy
            .WithOrigins(appOptions.FrontendUrl, "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    // ── Autenticación: validar JWT emitido por Supabase Auth ────────
    if (string.IsNullOrWhiteSpace(supabaseOptions.Url))
    {
        throw new InvalidOperationException(
            "Supabase:Url (variable SUPABASE_URL) no está configurado.");
    }

    var jwksConfigManager = new ConfigurationManager<JsonWebKeySet>(
        supabaseOptions.JwksUri,
        new JwksRetriever(),
        new HttpDocumentRetriever { RequireHttps = !builder.Environment.IsDevelopment() });

    builder.Services.AddSingleton(jwksConfigManager);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            // Sin esto, .NET remapea "sub"/"email" a los URIs largos de
            // ClaimTypes; lo desactivamos para poder leer los claims tal
            // como los emite Supabase (sub, email, role, user_metadata).
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = supabaseOptions.Authority,
                ValidateAudience = true,
                ValidAudience = "authenticated",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (_, _, kid, _) =>
                {
                    var jwks = jwksConfigManager.GetConfigurationAsync(CancellationToken.None)
                        .GetAwaiter().GetResult();
                    return string.IsNullOrEmpty(kid)
                        ? jwks.Keys
                        : jwks.Keys.Where(k => k.Kid == kid);
                }
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning(context.Exception, "Falló la validación del JWT de Supabase");
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddSingleton<IClaimsTransformation, SupabaseRoleClaimsTransformation>();

    // ── Cliente HTTP hacia Supabase Auth (refresh / logout) ─────────
    builder.Services.AddHttpClient(AuthService.SupabaseAuthHttpClientName, client =>
    {
        client.BaseAddress = new Uri($"{supabaseOptions.Authority}/");
        client.DefaultRequestHeaders.Add("apikey", supabaseOptions.AnonKey);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    });

    builder.Services.AddScoped<IAuthService, AuthService>();

    // ── Módulo Pedidos ───────────────────────────────────────────────
    builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
    builder.Services.AddScoped<IPedidoService, PedidoService>();

    // ── Módulo Productos + Inventario ────────────────────────────────
    builder.Services.AddScoped<ICategoriaService, CategoriaService>();
    builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
    builder.Services.AddScoped<IProductoService, ProductoService>();
    builder.Services.AddScoped<IInventarioRepository, InventarioRepository>();
    builder.Services.AddScoped<IInventarioService, InventarioService>();

    // ── Módulo Notas de Venta ────────────────────────────────────────
    builder.Services.AddScoped<INotaVentaRepository, NotaVentaRepository>();
    builder.Services.AddScoped<INotaVentaService, NotaVentaService>();

    // ── Módulo Reportes ──────────────────────────────────────────────
    builder.Services.AddScoped<IReporteService, ReporteService>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy
            .RequireClaim(SupabaseRoleClaimsTransformation.RoleClaimType, "admin"));

        options.AddPolicy("AdminOVendedor", policy => policy
            .RequireClaim(SupabaseRoleClaimsTransformation.RoleClaimType, "admin", "vendedor"));

        options.AddPolicy("AdminOVendedorODelivery", policy => policy
            .RequireClaim(SupabaseRoleClaimsTransformation.RoleClaimType, "admin", "vendedor", "delivery"));
    });

    // ── Swagger ──────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Punto Abasto API",
            Version = "v1",
            Description = "API de gestión de ventas, pedidos e inventario de Punto Abasto."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Token JWT emitido por Supabase Auth. Formato: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors(corsPolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(
        new HealthResponseDto("ok", DateTimeOffset.UtcNow, "1.0.0")
    )).AllowAnonymous();

    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Run($"http://0.0.0.0:{port}");
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente durante el arranque");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
