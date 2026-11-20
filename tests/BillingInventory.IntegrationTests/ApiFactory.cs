using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BillingInventory.IntegrationTests;

/// <summary>
/// Fábrica de la API en memoria para probar el pipeline HTTP completo.
/// Registra controladores de prueba que lanzan cada tipo de excepción.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Cadena de conexión a usar. Por defecto apunta a una base inexistente: sirve para las
    /// pruebas que no abren conexiones. Se sustituye cuando se necesita una base real.
    /// </summary>
    public string ConnectionString { get; init; } =
        "Server=localhost;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production: así se comprueba el comportamiento real ante errores (sin detalles internos).
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorsTestController).Assembly));
    }
}
