using System.Text.RegularExpressions;
using Serilog.Context;

namespace BillingInventory.Api.Middleware;

/// <summary>
/// Asigna un identificador de correlación a cada petición: reutiliza el de la cabecera
/// <c>X-Correlation-Id</c> si es válido, o genera uno nuevo. Lo devuelve en la respuesta
/// y lo agrega a todos los logs de la petición.
/// </summary>
/// <param name="next">Siguiente middleware.</param>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>Nombre de la cabecera HTTP.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Clave en <see cref="HttpContext.Items"/>.</summary>
    public const string ItemKey = "CorrelationId";

    /// <summary>Ejecuta el middleware.</summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <returns>Una tarea que representa la petición.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;

        // Se escribe en OnStarting porque el manejador de excepciones limpia las cabeceras de la respuesta.
        context.Response.OnStarting(
            static state =>
            {
                var (httpContext, id) = ((HttpContext, string))state;
                httpContext.Response.Headers[HeaderName] = id;
                return Task.CompletedTask;
            },
            (context, correlationId));

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        // Se valida el valor entrante para evitar inyección de saltos de línea o texto largo en los logs.
        var incoming = context.Request.Headers[HeaderName].ToString();
        return ValidIdRegex().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex ValidIdRegex();
}
