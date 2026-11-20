using System.Net;
using BillingInventory.Api.Authorization;
using BillingInventory.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingInventory.IntegrationTests.Security;

/// <summary>Un endpoint por política, solo para pruebas.</summary>
[ApiController]
[Route("test-policies")]
public sealed class PolicyTestController : ControllerBase
{
    [HttpGet("admin")]
    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult Admin() => Ok();

    [HttpGet("customers")]
    [Authorize(Policy = Policies.ManageCustomers)]
    public IActionResult Customers() => Ok();

    [HttpGet("products")]
    [Authorize(Policy = Policies.ManageProducts)]
    public IActionResult Products() => Ok();

    [HttpGet("inventory")]
    [Authorize(Policy = Policies.ManageInventory)]
    public IActionResult Inventory() => Ok();

    [HttpGet("invoices")]
    [Authorize(Policy = Policies.ManageInvoices)]
    public IActionResult Invoices() => Ok();

    [HttpGet("reports")]
    [Authorize(Policy = Policies.ViewReports)]
    public IActionResult Reports() => Ok();

    /// <summary>Sin atributo alguno: debe protegerse por la política por defecto.</summary>
    [HttpGet("unannotated")]
    public IActionResult Unannotated() => Ok();

    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() => Ok();
}

public sealed class PolicyMatrixTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Ruta, y qué roles tienen acceso. Admin siempre tiene acceso a todo.
    public static TheoryData<string, string, bool> Matrix()
    {
        var policies = new (string Route, string[] Allowed)[]
        {
            ("admin", [RoleNames.Admin]),
            ("customers", [RoleNames.Admin, RoleNames.Seller]),
            ("products", [RoleNames.Admin, RoleNames.Warehouse]),
            ("inventory", [RoleNames.Admin, RoleNames.Warehouse]),
            ("invoices", [RoleNames.Admin, RoleNames.Seller]),
            ("reports", [RoleNames.Admin, RoleNames.Seller]),
        };

        var data = new TheoryData<string, string, bool>();
        foreach (var (route, allowed) in policies)
        {
            foreach (var role in RoleNames.All)
            {
                data.Add(route, role, allowed.Contains(role));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Role_AccessMatchesPolicy(string route, string role, bool shouldAccess)
    {
        var response = await _client.SendAsync(TestTokens.Get($"/test-policies/{route}", TestTokens.For(role)));

        Assert.Equal(shouldAccess ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("customers")]
    [InlineData("products")]
    [InlineData("inventory")]
    [InlineData("invoices")]
    [InlineData("reports")]
    public async Task NoToken_Returns401(string route)
    {
        var response = await _client.SendAsync(TestTokens.Get($"/test-policies/{route}", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Forbidden_ReturnsProblemDetails403()
    {
        var response = await _client.SendAsync(
            TestTokens.Get("/test-policies/admin", TestTokens.For(RoleNames.Seller)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TokenWithUnknownRole_IsForbiddenEverywhere()
    {
        var response = await _client.SendAsync(
            TestTokens.Get("/test-policies/customers", TestTokens.For("SuperUsuario")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EndpointWithoutAttributes_RequiresAuthenticationByDefault()
    {
        var anonymous = await _client.SendAsync(TestTokens.Get("/test-policies/unannotated", null));
        var authenticated = await _client.SendAsync(
            TestTokens.Get("/test-policies/unannotated", TestTokens.For(RoleNames.Warehouse)));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
    }

    [Fact]
    public async Task AllowAnonymous_IsOpen()
    {
        var response = await _client.SendAsync(TestTokens.Get("/test-policies/public", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
