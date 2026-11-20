using BillingInventory.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingInventory.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Role"/> y datos iniciales de los tres roles del sistema.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Fecha fija: los datos semilla deben ser idénticos en cada migración.
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Identificador del rol <see cref="RoleNames.Admin"/>.</summary>
    public const int AdminId = 1;

    /// <summary>Identificador del rol <see cref="RoleNames.Seller"/>.</summary>
    public const int SellerId = 2;

    /// <summary>Identificador del rol <see cref="RoleNames.Warehouse"/>.</summary>
    public const int WarehouseId = 3;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role { Id = AdminId, Name = RoleNames.Admin, CreatedAtUtc = SeedDate },
            new Role { Id = SellerId, Name = RoleNames.Seller, CreatedAtUtc = SeedDate },
            new Role { Id = WarehouseId, Name = RoleNames.Warehouse, CreatedAtUtc = SeedDate });
    }
}
