using Microsoft.OpenApi;

namespace BillingInventory.Api.Extensions;

/// <summary>Configuración de Swagger/OpenAPI.</summary>
public static class SwaggerExtensions
{
    private static readonly string[] XmlDocFiles = ["BillingInventory.Api.xml", "BillingInventory.Application.xml"];

    /// <summary>
    /// Registra la generación de documentación OpenAPI con comentarios XML de las capas
    /// Api y Application, y la definición del esquema de seguridad Bearer (JWT).
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API de Facturación e Inventario",
                Version = "v1",
                Description =
                    "API REST para gestionar clientes, productos, inventario, facturas con ITBIS, " +
                    "reportes de ventas y facturas en PDF.",
            });

            // Los DTOs viven en Application, por eso también se incluye su XML.
            foreach (var xmlFile in XmlDocFiles)
            {
                var path = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(path))
                {
                    options.IncludeXmlComments(path, includeControllerXmlComments: true);
                }
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT obtenido en el login. Pegue solo el token, sin el prefijo \"Bearer\".",
            });
        });

        return services;
    }

    /// <summary>
    /// Publica el JSON de OpenAPI y la interfaz de Swagger UI, y redirige la raíz a ella.
    /// </summary>
    /// <param name="app">Aplicación web.</param>
    /// <returns>La misma aplicación, para encadenar llamadas.</returns>
    public static WebApplication UseSwaggerDocs(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Facturación e Inventario v1");
            options.DocumentTitle = "Facturación e Inventario - API";
            options.DisplayRequestDuration();
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }
}
