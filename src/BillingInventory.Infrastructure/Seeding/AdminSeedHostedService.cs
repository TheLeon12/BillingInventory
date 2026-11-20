using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingInventory.Infrastructure.Seeding;

/// <summary>Ejecuta <see cref="AdminSeeder"/> al arrancar la aplicación, antes de aceptar peticiones.</summary>
/// <param name="scopes">Fábrica de ámbitos, porque el sembrador usa el contexto de EF (servicio con ámbito).</param>
/// <param name="logger">Logger.</param>
public sealed class AdminSeedHostedService(IServiceScopeFactory scopes, ILogger<AdminSeedHostedService> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();

        try
        {
            await seeder.SeedAsync(cancellationToken);
        }
        catch (SeedConfigurationException)
        {
            // Un error de configuración debe impedir el arranque: es mejor fallar claro que no tener administrador.
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo de entorno (base no disponible, migraciones sin aplicar) no debe tumbar la API:
            // /health/ready ya reporta la base de datos. Se registra con detalle para poder corregirlo.
            logger.LogError(
                ex,
                "No se pudo crear el administrador inicial. ¿Está disponible la base de datos y se aplicaron las migraciones?");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
