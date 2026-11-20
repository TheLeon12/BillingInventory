using System.Globalization;
using System.Text.Json.Serialization;
using BillingInventory.Api.Authorization;
using BillingInventory.Api.Extensions;
using BillingInventory.Api.Middleware;
using BillingInventory.Application;
using BillingInventory.Infrastructure;
using Microsoft.Extensions.Hosting;
using Serilog;

// Logger mínimo para capturar fallos que ocurran antes de leer la configuración.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            // Enumeraciones como texto ("Desc", "Emitida") en lugar de números: más legible para los clientes.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddJwtAuthentication();
    builder.Services.AddAppAuthorization();
    builder.Services.AddAuthRateLimiting(builder.Configuration);
    builder.Services.AddSwaggerDocs();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id
                ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["correlationId"] =
                context.HttpContext.Items[CorrelationIdMiddleware.ItemKey];
        });

    var app = builder.Build();

    // El orden importa: la correlación debe envolver al log de peticiones y al manejador de errores.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerDocs();
    }

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException la lanzan las herramientas de EF a propósito; no es un fallo.
    Log.Fatal(ex, "La aplicación terminó inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Punto de entrada de la API. Se declara parcial para que las pruebas de
/// integración puedan usar <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
