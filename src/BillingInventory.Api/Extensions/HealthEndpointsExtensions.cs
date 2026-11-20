using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BillingInventory.Api.Extensions;

/// <summary>Endpoints de salud de la API.</summary>
public static class HealthEndpointsExtensions
{
    /// <summary>
    /// Mapea <c>/health</c> (la API está viva, sin dependencias) y <c>/health/ready</c>
    /// (la API puede atender peticiones: verifica la base de datos).
    /// </summary>
    /// <param name="app">Aplicación web.</param>
    /// <returns>La misma aplicación, para encadenar llamadas.</returns>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteJsonAsync,
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteJsonAsync,
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        // No se incluyen excepciones ni cadenas de conexión: solo el estado de cada chequeo.
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
