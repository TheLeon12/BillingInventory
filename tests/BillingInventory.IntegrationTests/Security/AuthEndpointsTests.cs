using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BillingInventory.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BillingInventory.IntegrationTests.Security;

[Collection(SqlServerCollection.Name)]
public sealed class AuthEndpointsTests : IDisposable
{
    private const string Password = "Clave#2026";

    private readonly SqlServerFixture _sql;
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(SqlServerFixture sql)
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

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@prueba.test";

    private Task<HttpResponseMessage> RegisterAsync(string email, string password = Password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, fullName = "Usuario Prueba", password });

    private Task<HttpResponseMessage> LoginAsync(string email, string password = Password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<string> RegisterAndLoginAsync(string? email = null)
    {
        email ??= UniqueEmail();
        (await RegisterAsync(email)).EnsureSuccessStatusCode();
        var login = await (await LoginAsync(email)).Content.ReadFromJsonAsync<JsonElement>();
        return login.GetProperty("accessToken").GetString()!;
    }

    // ---------- Registro ----------

    [Fact]
    public async Task Register_Valid_Returns201WithSellerRoleAndNoPasswordData()
    {
        var email = UniqueEmail();

        var response = await RegisterAsync(email);
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal(RoleNames.Seller, body.GetProperty("role").GetString());
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        await using var context = _sql.CreateContext();
        var stored = await context.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.DoesNotContain(Password, stored.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_IgnoresRoleSentByClient()
    {
        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Intruso",
            password = Password,
            role = "Admin",
            roleId = 1,
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(RoleNames.Seller, body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_IgnoringCaseAndSpaces_Returns409()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await RegisterAsync($"  {email.ToUpperInvariant()}  ");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidData_Returns400WithErrorsPerField()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "no-es-correo", fullName = "", password = "debil" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("FullName", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_Valid_ReturnsJwtWithExpectedClaims()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await LoginAsync(email.ToUpperInvariant());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.Equal(ApiFactory.JwtIssuer, jwt.Issuer);
        Assert.Contains(ApiFactory.JwtAudience, jwt.Audiences);
        Assert.Equal(RoleNames.Seller, jwt.GetClaim("role").Value);
        Assert.Equal(email, jwt.GetClaim("email").Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(50));
        Assert.Equal(RoleNames.Seller, body.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameGeneric401()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var wrongPassword = await LoginAsync(email, "OtraClave#1");
        var unknownEmail = await LoginAsync(UniqueEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        var a = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var b = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>();
        // Mismo detalle: no se revela si el correo existe.
        Assert.Equal(a.GetProperty("detail").GetString(), b.GetProperty("detail").GetString());
        Assert.Equal("application/problem+json", wrongPassword.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Bearer", wrongPassword.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401EvenWithCorrectPassword()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);
        await using (var context = _sql.CreateContext())
        {
            var user = await context.Users.SingleAsync(u => u.Email == email);
            user.IsActive = false;
            await context.SaveChangesAsync();
        }

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithOutdatedHash_SucceedsAndRehashesPassword()
    {
        // Hash generado con pocas iteraciones: Identity lo reconoce como antiguo.
        var weakHasher = new PasswordHasher<object>(
            Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
        var email = UniqueEmail();
        string oldHash;
        await using (var context = _sql.CreateContext())
        {
            oldHash = weakHasher.HashPassword(new object(), Password);
            context.Users.Add(new User { Email = email, FullName = "Antiguo", PasswordHash = oldHash, RoleId = 2 });
            await context.SaveChangesAsync();
        }

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var check = _sql.CreateContext();
        Assert.NotEqual(oldHash, (await check.Users.SingleAsync(u => u.Email == email)).PasswordHash);
    }

    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429WithProblemDetails()
    {
        using var limited = new ApiFactory { ConnectionString = _sql.ConnectionString, AuthPermitPerMinute = 3 };
        using var client = limited.CreateClient();
        HttpResponseMessage? last = null;

        for (var i = 0; i < 4; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new { email = UniqueEmail(), password = "x" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("application/problem+json", last.Content.Headers.ContentType?.MediaType);
    }

    // ---------- Tokens en endpoints protegidos ----------

    [Fact]
    public async Task Me_WithoutToken_Returns401ProblemDetails()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserFromClaims()
    {
        var email = UniqueEmail();
        var token = await RegisterAndLoginAsync(email);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal(RoleNames.Seller, body.GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-issuer")]
    [InlineData("expired")]
    public async Task Me_WithForgedOrExpiredToken_Returns401(string defect)
    {
        var token = CraftToken(defect);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var token = await RegisterAndLoginAsync();
        var tampered = token[..^3] + (token.EndsWith("AAA", StringComparison.Ordinal) ? "BBB" : "AAA");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Crea un token que es válido salvo por un defecto concreto.</summary>
    private static string CraftToken(string defect)
    {
        var now = DateTime.UtcNow;
        var key = defect == "wrong-key"
            ? "otra-clave-distinta-0123456789-abcdefghijklmnopqrstuvwxyz-ABCDEFGH"
            : ApiFactory.JwtKey;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = defect == "wrong-issuer" ? "alguien-mas" : ApiFactory.JwtIssuer,
            Audience = defect == "wrong-audience" ? "otra-audiencia" : ApiFactory.JwtAudience,
            IssuedAt = defect == "expired" ? now.AddHours(-3) : now,
            NotBefore = defect == "expired" ? now.AddHours(-3) : now,
            Expires = defect == "expired" ? now.AddHours(-2) : now.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = "1",
                ["email"] = "falso@prueba.test",
                ["name"] = "Falso",
                ["role"] = RoleNames.Admin,
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
