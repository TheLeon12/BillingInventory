using BillingInventory.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.IntegrationTests.Security;

[Collection(SqlServerCollection.Name)]
public sealed class UserPersistenceTests(SqlServerFixture sql)
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static User NewUser(int roleId = 2) => new()
    {
        Email = $"{Guid.NewGuid():N}@prueba.test",
        FullName = "Usuario de Prueba",
        PasswordHash = "hash-de-prueba",
        RoleId = roleId,
    };

    [Fact]
    public async Task Migration_SeedsTheThreeSystemRoles()
    {
        await using var context = sql.CreateContext();

        var names = await context.Roles.OrderBy(r => r.Id).Select(r => r.Name).ToListAsync();

        Assert.Equal([RoleNames.Admin, RoleNames.Seller, RoleNames.Warehouse], names);
    }

    [Fact]
    public async Task SavingUser_StampsCreatedAtUtc_AndLeavesUpdatedAtNull()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        await using var context = sql.CreateContext(clock);
        var user = NewUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), user.CreatedAtUtc);
        Assert.Null(user.UpdatedAtUtc);
        Assert.NotEmpty(user.RowVersion);
    }

    [Fact]
    public async Task ModifyingUser_StampsUpdatedAtUtc_WithoutChangingCreatedAt()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        await using var context = sql.CreateContext(clock);
        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        clock.Now = clock.Now.AddHours(5);
        user.FullName = "Nombre Nuevo";
        await context.SaveChangesAsync();

        Assert.Equal(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), user.CreatedAtUtc);
        Assert.Equal(new DateTime(2026, 3, 15, 15, 0, 0, DateTimeKind.Utc), user.UpdatedAtUtc);
    }

    [Fact]
    public async Task DuplicateEmail_IsRejectedByUniqueIndex()
    {
        await using var context = sql.CreateContext();
        var first = NewUser();
        context.Users.Add(first);
        await context.SaveChangesAsync();

        await using var other = sql.CreateContext();
        other.Users.Add(new User
        {
            Email = first.Email.ToUpperInvariant(), // SQL Server compara sin distinguir mayúsculas
            FullName = "Otro",
            PasswordHash = "x",
            RoleId = 2,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
    }

    [Fact]
    public async Task UserWithUnknownRole_IsRejectedByForeignKey()
    {
        await using var context = sql.CreateContext();
        context.Users.Add(NewUser(roleId: 999));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentUpdates_SecondSaveFailsWithConcurrencyException()
    {
        await using var setup = sql.CreateContext();
        var created = NewUser();
        setup.Users.Add(created);
        await setup.SaveChangesAsync();

        await using var first = sql.CreateContext();
        await using var second = sql.CreateContext();
        var userInFirst = await first.Users.SingleAsync(u => u.Id == created.Id);
        var userInSecond = await second.Users.SingleAsync(u => u.Id == created.Id);

        userInFirst.FullName = "Cambio A";
        await first.SaveChangesAsync();

        userInSecond.FullName = "Cambio B";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
