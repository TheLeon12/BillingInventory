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
    /// <summary>Emisor JWT de las pruebas.</summary>
    public const string JwtIssuer = "BillingInventory.Tests";

    /// <summary>Audiencia JWT de las pruebas.</summary>
    public const string JwtAudience = "BillingInventory.Tests.Clients";

    /// <summary>Clave JWT de las pruebas (valor ficticio, solo para este proyecto).</summary>
    public const string JwtKey = "clave-de-integracion-solo-para-pruebas-0123456789-abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Cadena de conexión a usar. Por defecto apunta a una base inexistente: sirve para las
    /// pruebas que no abren conexiones. Se sustituye cuando se necesita una base real.
    /// </summary>
    public string ConnectionString { get; init; } =
        "Server=localhost;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Intentos por minuto permitidos en login y registro. Alto por defecto para no interferir.</summary>
    public int AuthPermitPerMinute { get; init; } = 1000;

    /// <summary>Valores de configuración adicionales (por ejemplo, la sección <c>Seed</c>).</summary>
    public IReadOnlyDictionary<string, string> ExtraSettings { get; init; } = new Dictionary<string, string>();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in ExtraSettings)
        {
            builder.UseSetting(key, value);
        }

        // Production: así se comprueba el comportamiento real ante errores (sin detalles internos).
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", AuthPermitPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));

        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorsTestController).Assembly));
    }
}
