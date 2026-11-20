using BillingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace BillingInventory.IntegrationTests;

/// <summary>
/// Levanta un SQL Server 2022 efímero en Docker, compartido por todas las pruebas de la colección.
/// Se usa SQL Server real (no InMemory) para validar transacciones y concurrencia.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>Cadena de conexión del contenedor.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Crea un contexto nuevo apuntando al contenedor.</summary>
    /// <param name="clock">Reloj a usar; por defecto el del sistema.</param>
    public AppDbContext CreateContext(TimeProvider? clock = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options, clock ?? TimeProvider.System);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _container.DisposeAsync();
}

/// <summary>Colección que comparte <see cref="SqlServerFixture"/> entre clases de prueba.</summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    /// <summary>Nombre de la colección.</summary>
    public const string Name = "SqlServer";
}
