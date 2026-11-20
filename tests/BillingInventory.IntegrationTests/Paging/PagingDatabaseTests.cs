using BillingInventory.Application.Common.Paging;
using BillingInventory.Domain.Common;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.IntegrationTests.Paging;

/// <summary>Verifica contra SQL Server real que la paginación y el orden se traducen bien a SQL.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class PagingDatabaseTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const int Total = 25;

    private static readonly SortMap<Widget> Map = new SortMap<Widget>("name")
        .Add("name", w => w.Name)
        .Add("price", w => w.Price);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Solo 5 precios distintos para 25 filas: obliga a depender del desempate por Id.
        context.Widgets.AddRange(Enumerable.Range(1, Total).Select(i => new Widget
        {
            Name = $"W{i:00}",
            Price = (i % 5) * 10m,
        }));
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MiddlePage_ReturnsItemsAndMetadata()
    {
        await using var context = CreateContext();

        var result = await Map.Apply(context.Widgets, null, null).ToPagedResultAsync(2, 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(Total, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
        Assert.Equal("W11", result.Items[0].Name);
    }

    [Fact]
    public async Task LastPage_IsPartial()
    {
        await using var context = CreateContext();

        var result = await Map.Apply(context.Widgets, null, null).ToPagedResultAsync(3, 10);

        Assert.Equal(5, result.Items.Count);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task PageBeyondTheEnd_ReturnsEmptyItemsButKeepsTotal()
    {
        await using var context = CreateContext();

        var result = await Map.Apply(context.Widgets, null, null).ToPagedResultAsync(99, 10);

        Assert.Empty(result.Items);
        Assert.Equal(Total, result.TotalCount);
    }

    [Fact]
    public async Task SortingByColumnWithTies_NeverRepeatsOrSkipsRowsAcrossPages()
    {
        await using var context = CreateContext();
        var seen = new List<int>();

        for (var page = 1; page <= 5; page++)
        {
            var result = await Map.Apply(context.Widgets, "price", SortDirection.Desc).ToPagedResultAsync(page, 5);
            seen.AddRange(result.Items.Select(w => w.Id));
        }

        Assert.Equal(Total, seen.Count);
        Assert.Equal(Total, seen.Distinct().Count());
    }

    [Fact]
    public async Task Filter_AffectsTotalCount()
    {
        await using var context = CreateContext();

        var result = await Map.Apply(context.Widgets.WhereIf(true, w => w.Price >= 30m), null, null)
            .ToPagedResultAsync(1, 20);

        // Precios 30 y 40: 5 filas de cada uno.
        Assert.Equal(10, result.TotalCount);
        Assert.All(result.Items, w => Assert.True(w.Price >= 30m));
    }

    [Fact]
    public async Task UnknownSortField_IsRejectedBeforeReachingTheDatabase()
    {
        await using var context = CreateContext();

        Assert.Throws<ValidationException>(() => Map.Apply(context.Widgets, "Name; DROP TABLE Widgets", null));
        await Task.CompletedTask;
    }

    private TestDbContext CreateContext()
    {
        var connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PagingTests" };
        return new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
    }

    private sealed class Widget : BaseEntity
    {
        public string Name { get; set; } = "";

        public decimal Price { get; set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>().Property(w => w.RowVersion).IsRowVersion();
            modelBuilder.Entity<Widget>().Property(w => w.Price).HasPrecision(18, 2);
        }
    }
}
