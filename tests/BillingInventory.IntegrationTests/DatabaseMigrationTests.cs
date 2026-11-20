using Microsoft.EntityFrameworkCore;

namespace BillingInventory.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class DatabaseMigrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Migrations_ApplyCleanly_AndLeaveNoPendingChanges()
    {
        await using var context = fixture.CreateContext();

        var canConnect = await context.Database.CanConnectAsync();
        var pending = await context.Database.GetPendingMigrationsAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.True(canConnect);
        Assert.Empty(pending);
        Assert.Contains(applied, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }
}
