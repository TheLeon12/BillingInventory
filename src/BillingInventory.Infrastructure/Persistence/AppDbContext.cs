using BillingInventory.Application.Abstractions;
using BillingInventory.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core (Code First) sobre SQL Server. Sella la auditoría de las entidades
/// y configura convenciones comunes: <c>RowVersion</c> y precisión de <see cref="decimal"/>.
/// </summary>
/// <param name="options">Opciones del contexto.</param>
/// <param name="clock">Reloj inyectable, para poder controlar el tiempo en pruebas.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider clock)
    : DbContext(options), IAppDbContext
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Toda entidad hereda el token de concurrencia optimista.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsRowVersion();
        }
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Los importes siempre son decimal(18,2); nunca se deja la precisión por defecto.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
