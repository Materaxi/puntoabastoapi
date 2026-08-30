using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace PuntoAbasto.Api.Middleware;

/// <summary>
/// Captura cualquier excepción no manejada por los controllers y la traduce
/// a un ProblemDetails (RFC 7807) en vez de dejar que ASP.NET devuelva un
/// stack trace crudo o un 500 sin cuerpo.
///
/// También desempaqueta PostgresException (EF la envuelve en
/// DbUpdateException): los triggers de negocio en schema.sql usan
/// RAISE EXCEPTION para rechazar operaciones inválidas (p. ej. "Stock
/// insuficiente..." al confirmar un pedido), y sin esto esos mensajes
/// se perderían detrás de un 500 genérico.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);

            var (status, title, detail) = Clasificar(ex, _environment.IsDevelopment());

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.com/{status}"
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int Status, string Title, string? Detail) Clasificar(Exception ex, bool isDevelopment)
    {
        var postgres = ex as PostgresException ?? ex.InnerException as PostgresException;
        if (postgres is not null)
        {
            return postgres.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => (StatusCodes.Status409Conflict, "Registro duplicado", postgres.MessageText),
                PostgresErrorCodes.ForeignKeyViolation => (StatusCodes.Status409Conflict, "Referencia inválida", postgres.MessageText),
                PostgresErrorCodes.CheckViolation => (StatusCodes.Status400BadRequest, "Dato inválido", postgres.MessageText),
                "P0001" => (StatusCodes.Status409Conflict, "Regla de negocio violada", postgres.MessageText),
                _ => (StatusCodes.Status500InternalServerError, "Error de base de datos", isDevelopment ? postgres.MessageText : null)
            };
        }

        return ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", ex.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Operación inválida", ex.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida", ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acceso denegado", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado", isDevelopment ? ex.Message : null)
        };
    }
}
