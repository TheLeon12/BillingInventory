using BillingInventory.Application.Abstractions;
using BillingInventory.Infrastructure.Persistence;
using BillingInventory.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingInventory.Infrastructure;

/// <summary>Registro de los servicios de la capa Infrastructure.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el contexto de EF Core y sus dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración con <c>ConnectionStrings:DefaultConnection</c>.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    /// <exception cref="InvalidOperationException">Falta la cadena de conexión.</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:DefaultConnection. Configúrela con user-secrets (ver README).");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Chequeo de disponibilidad: etiqueta "ready" para distinguirlo del chequeo de vida (/health).
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        return services;
    }
}
