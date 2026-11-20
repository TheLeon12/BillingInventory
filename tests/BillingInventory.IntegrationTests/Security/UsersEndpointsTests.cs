using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillingInventory.Domain.Security;
using BillingInventory.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.IntegrationTests.Security;

[Collection(SqlServerCollection.Name)]
public sealed class UsersEndpointsTests : IDisposable
{
    private const string Password = "Clave#2026";

    private readonly SqlServerFixture _sql;
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public UsersEndpointsTests(SqlServerFixture sql)
    {
        _sql = sql;
        _factory = new ApiFactory { ConnectionString = sql.ConnectionString };
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>Inserta un usuario directamente en la base de datos.</summary>
    private async Task<User> CreateUserAsync(string role = RoleNames.Seller, bool active = true, string? tag = null)
    {
        await using var context = _sql.CreateContext();
        var roleId = (await context.Roles.SingleAsync(r => r.Name == role)).Id;
        var user = new User
        {
            Email = $"{tag ?? "u"}-{Guid.NewGuid():N}@prueba.test",
            FullName = $"Usuario {tag}",
            PasswordHash = new IdentityPasswordHasher().Hash(Password),
            IsActive = active,
            RoleId = roleId,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static string AdminToken(int adminId) => TestTokens.For(RoleNames.Admin, adminId);

    private async Task<JsonElement> GetPageAsync(string query, int adminId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/users?{query}", AdminToken(adminId));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ---------- Autorización ----------

    [Theory]
    [InlineData(RoleNames.Seller)]
    [InlineData(RoleNames.Warehouse)]
    public async Task NonAdminRoles_AreForbiddenOnEveryEndpoint(string role)
    {
        var token = TestTokens.For(role, 5);

        foreach (var (method, url) in new[]
        {
            (HttpMethod.Get, "/api/users"),
            (HttpMethod.Get, "/api/users/1"),
            (HttpMethod.Put, "/api/users/1/role"),
            (HttpMethod.Put, "/api/users/1/status"),
        })
        {
            var response = await SendAsync(method, url, token, method == HttpMethod.Put ? new { role = "Admin", isActive = true } : null);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/users", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Listado ----------

    [Fact]
    public async Task List_PaginatesSortsAndReturnsMetadata()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var tag = $"pag{Guid.NewGuid():N}"[..12];
        for (var i = 0; i < 5; i++)
        {
            await CreateUserAsync(tag: tag);
        }

        var page = await GetPageAsync($"search={tag}&pageSize=2&page=2&sortBy=email&sortDirection=asc", admin.Id);

        Assert.Equal(5, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.True(page.GetProperty("hasNext").GetBoolean());
        Assert.True(page.GetProperty("hasPrevious").GetBoolean());
        var emails = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("email").GetString()).ToList();
        Assert.Equal(emails.Order(StringComparer.Ordinal).ToList(), emails);
    }

    [Fact]
    public async Task List_FiltersByRoleCaseInsensitiveAndByActiveFlag()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var tag = $"fil{Guid.NewGuid():N}"[..12];
        await CreateUserAsync(RoleNames.Warehouse, tag: tag);
        await CreateUserAsync(RoleNames.Warehouse, active: false, tag: tag);
        await CreateUserAsync(RoleNames.Seller, tag: tag);

        var warehouse = await GetPageAsync($"search={tag}&role=almacen", admin.Id);
        var inactive = await GetPageAsync($"search={tag}&isActive=false", admin.Id);

        Assert.Equal(2, warehouse.GetProperty("totalCount").GetInt32());
        Assert.All(warehouse.GetProperty("items").EnumerateArray(), i => Assert.Equal(RoleNames.Warehouse, i.GetProperty("role").GetString()));
        Assert.Equal(1, inactive.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("sortBy=passwordHash", "sortBy")]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("page=0", "Page")]
    [InlineData("role=Hacker", "Role")]
    public async Task List_InvalidParameters_Return400WithFieldError(string query, string field)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/users?{query}", AdminToken(1));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task GetById_ReturnsUserWithoutPasswordData_Or404()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var target = await CreateUserAsync();

        var found = await SendAsync(HttpMethod.Get, $"/api/users/{target.Id}", AdminToken(admin.Id));
        var text = await found.Content.ReadAsStringAsync();
        var missing = await SendAsync(HttpMethod.Get, $"/api/users/{int.MaxValue}", AdminToken(admin.Id));

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Contains(target.Email, text, StringComparison.Ordinal);
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // ---------- Cambio de rol ----------

    [Fact]
    public async Task ChangeRole_UpdatesRole_AndNextLoginCarriesTheNewRole()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var target = await CreateUserAsync();

        var response = await SendAsync(HttpMethod.Put, $"/api/users/{target.Id}/role", AdminToken(admin.Id), new { role = "almacen" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(RoleNames.Warehouse, body.GetProperty("role").GetString());

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = target.Email, password = Password });
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RoleNames.Warehouse, loginBody.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task ChangeRole_InvalidRoleUnknownUserAndSelf_AreRejected()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var target = await CreateUserAsync();
        var token = AdminToken(admin.Id);

        var invalidRole = await SendAsync(HttpMethod.Put, $"/api/users/{target.Id}/role", token, new { role = "Dios" });
        var unknownUser = await SendAsync(HttpMethod.Put, $"/api/users/{int.MaxValue}/role", token, new { role = "Admin" });
        var self = await SendAsync(HttpMethod.Put, $"/api/users/{admin.Id}/role", token, new { role = "Vendedor" });

        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, self.StatusCode);

        await using var context = _sql.CreateContext();
        Assert.Equal(RoleNames.Admin, (await context.Users.Include(u => u.Role).SingleAsync(u => u.Id == admin.Id)).Role.Name);
    }

    // ---------- Activar y desactivar ----------

    [Fact]
    public async Task Deactivate_BlocksLogin_AndReactivateRestoresIt()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var target = await CreateUserAsync();
        var token = AdminToken(admin.Id);

        var off = await SendAsync(HttpMethod.Put, $"/api/users/{target.Id}/status", token, new { isActive = false });
        var blocked = await _client.PostAsJsonAsync("/api/auth/login", new { email = target.Email, password = Password });
        var on = await SendAsync(HttpMethod.Put, $"/api/users/{target.Id}/status", token, new { isActive = true });
        var allowed = await _client.PostAsJsonAsync("/api/auth/login", new { email = target.Email, password = Password });

        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await off.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Self_Returns422_ButSelfActivateIsHarmless()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);
        var token = AdminToken(admin.Id);

        var off = await SendAsync(HttpMethod.Put, $"/api/users/{admin.Id}/status", token, new { isActive = false });
        var on = await SendAsync(HttpMethod.Put, $"/api/users/{admin.Id}/status", token, new { isActive = true });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, off.StatusCode);
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
    }

    [Fact]
    public async Task SetStatus_UnknownUser_Returns404()
    {
        var admin = await CreateUserAsync(RoleNames.Admin);

        var response = await SendAsync(HttpMethod.Put, $"/api/users/{int.MaxValue}/status", AdminToken(admin.Id), new { isActive = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
