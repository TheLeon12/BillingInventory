using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillingInventory.Application.Abstractions;
using BillingInventory.Domain.Security;
using BillingInventory.Infrastructure.Persistence;
using BillingInventory.Infrastructure.Security;
using BillingInventory.Infrastructure.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BillingInventory.IntegrationTests.Security;

/// <summary>
/// Usa una base de datos propia ("SeedTests") porque el sembrador depende de que no exista ningún
/// administrador, y otras pruebas crean administradores en la base compartida.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class AdminSeederTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string AdminEmail = "Admin.Inicial@Prueba.Test";
    private const string AdminPassword = "Inicial#2026";

    private string SeedDbConnectionString =>
        new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "SeedTests" }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext(SeedDbConnectionString);
        await context.Database.MigrateAsync();
        await context.Users.ExecuteDeleteAsync(); // cada prueba parte sin usuarios
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static AppDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options, TimeProvider.System);

    private static AdminSeeder CreateSeeder(AppDbContext context, string? email, string? password, string fullName = "Admin Inicial") =>
        new(
            context,
            new IdentityPasswordHasher(),
            Options.Create(new SeedOptions { AdminEmail = email, AdminPassword = password, AdminFullName = fullName }),
            NullLogger<AdminSeeder>.Instance);

    private async Task AddUserAsync(string email, string role, string password = "Otra#Clave1")
    {
        await using var context = CreateContext(SeedDbConnectionString);
        var roleId = (await context.Roles.SingleAsync(r => r.Name == role)).Id;
        context.Users.Add(new User
        {
            Email = email,
            FullName = "Existente",
            PasswordHash = new IdentityPasswordHasher().Hash(password),
            RoleId = roleId,
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task NotConfigured_DoesNothing_AndNeverTouchesTheDatabase()
    {
        // Base inaccesible a propósito: si el sembrador la consultara, fallaría.
        await using var unreachable = CreateContext("Server=localhost,1;Database=x;User Id=sa;Password=NoImporta1!;Connect Timeout=2;TrustServerCertificate=True");

        var result = await CreateSeeder(unreachable, email: null, password: null).SeedAsync();

        Assert.Equal(AdminSeedResult.NotConfigured, result);
    }

    [Fact]
    public async Task NoAdminExists_CreatesActiveAdminWithHashedPasswordAndNormalizedEmail()
    {
        await using var context = CreateContext(SeedDbConnectionString);

        var result = await CreateSeeder(context, AdminEmail, AdminPassword).SeedAsync();

        Assert.Equal(AdminSeedResult.Created, result);
        await using var check = CreateContext(SeedDbConnectionString);
        var admin = await check.Users.Include(u => u.Role).SingleAsync();
        Assert.Equal("admin.inicial@prueba.test", admin.Email);
        Assert.Equal(RoleNames.Admin, admin.Role.Name);
        Assert.True(admin.IsActive);
        Assert.NotEqual(AdminPassword, admin.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, new IdentityPasswordHasher().Verify(admin.PasswordHash, AdminPassword));
    }

    [Fact]
    public async Task RunningTwice_IsIdempotent_AndNeverResetsThePassword()
    {
        await using var first = CreateContext(SeedDbConnectionString);
        await CreateSeeder(first, AdminEmail, AdminPassword).SeedAsync();
        string hashAfterFirst;
        await using (var read = CreateContext(SeedDbConnectionString))
        {
            hashAfterFirst = (await read.Users.SingleAsync()).PasswordHash;
        }

        await using var second = CreateContext(SeedDbConnectionString);
        var result = await CreateSeeder(second, AdminEmail, "OtraClave#999").SeedAsync();

        Assert.Equal(AdminSeedResult.AdminAlreadyExists, result);
        await using var check = CreateContext(SeedDbConnectionString);
        Assert.Equal(hashAfterFirst, (await check.Users.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task AnotherAdminExists_DoesNotCreateTheConfiguredOne()
    {
        await AddUserAsync("otro.admin@prueba.test", RoleNames.Admin);
        await using var context = CreateContext(SeedDbConnectionString);

        var result = await CreateSeeder(context, AdminEmail, AdminPassword).SeedAsync();

        Assert.Equal(AdminSeedResult.AdminAlreadyExists, result);
        await using var check = CreateContext(SeedDbConnectionString);
        Assert.Equal(1, await check.Users.CountAsync());
    }

    [Fact]
    public async Task ConfiguredEmailBelongsToNonAdmin_IsNeverPromoted()
    {
        // Escenario de ataque: alguien registró el correo del admin antes del primer arranque.
        await AddUserAsync("admin.inicial@prueba.test", RoleNames.Seller, password: "ClaveDelAtacante#1");
        await using var context = CreateContext(SeedDbConnectionString);

        var result = await CreateSeeder(context, AdminEmail, AdminPassword).SeedAsync();

        Assert.Equal(AdminSeedResult.EmailInUse, result);
        await using var check = CreateContext(SeedDbConnectionString);
        var user = await check.Users.Include(u => u.Role).SingleAsync();
        Assert.Equal(RoleNames.Seller, user.Role.Name);
        Assert.False(await check.Users.AnyAsync(u => u.Role.Name == RoleNames.Admin));
    }

    [Theory]
    [InlineData("admin@prueba.test", "debil")]
    [InlineData("no-es-correo", "Inicial#2026")]
    [InlineData("admin@prueba.test", null)]
    [InlineData(null, "Inicial#2026")]
    public async Task InvalidOrPartialConfiguration_Throws_WithoutLeakingThePassword(string? email, string? password)
    {
        await using var context = CreateContext(SeedDbConnectionString);

        var ex = await Assert.ThrowsAsync<SeedConfigurationException>(
            () => CreateSeeder(context, email, password).SeedAsync());

        if (password is not null)
        {
            Assert.DoesNotContain(password, ex.Message, StringComparison.Ordinal);
        }

        await using var check = CreateContext(SeedDbConnectionString);
        Assert.Equal(0, await check.Users.CountAsync());
    }

    [Fact]
    public async Task ApiStartup_SeedsAdmin_WhoCanLoginAndManageUsers()
    {
        using var factory = new ApiFactory
        {
            ConnectionString = SeedDbConnectionString,
            ExtraSettings = new Dictionary<string, string>
            {
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
            },
        };
        using var client = factory.CreateClient(); // al crear el cliente arranca el host y corre el sembrador

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = AdminPassword });
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(RoleNames.Admin, loginBody.GetProperty("user").GetProperty("role").GetString());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.GetProperty("accessToken").GetString());
        var users = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }

    [Fact]
    public async Task ApiStartup_WithInvalidSeedConfiguration_FailsToStart()
    {
        using var factory = new ApiFactory
        {
            ConnectionString = SeedDbConnectionString,
            ExtraSettings = new Dictionary<string, string>
            {
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = "debil",
            },
        };

        await Assert.ThrowsAnyAsync<Exception>(() => Task.Run(() => factory.CreateClient()));
    }

    [Fact]
    public async Task ApiStartup_WhenDatabaseIsUnavailable_StillStarts()
    {
        using var factory = new ApiFactory
        {
            ConnectionString = "Server=localhost,1;Database=x;User Id=sa;Password=NoImporta1!;Connect Timeout=2;TrustServerCertificate=True",
            ExtraSettings = new Dictionary<string, string>
            {
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
            },
        };
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        await Task.CompletedTask;
    }
}
