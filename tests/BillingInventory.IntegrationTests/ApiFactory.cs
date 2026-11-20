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
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production: así se comprueba el comportamiento real ante errores (sin detalles internos).
        builder.UseEnvironment("Production");

        // La cadena no se usa en estas pruebas (no se abre ninguna conexión); solo evita el fallo de arranque.
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=localhost;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True");

        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorsTestController).Assembly));
    }
}
