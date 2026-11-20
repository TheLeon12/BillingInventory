using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingInventory.Application.Common.Paging;
using Microsoft.AspNetCore.Mvc;

namespace BillingInventory.IntegrationTests.Paging;

/// <summary>Controlador solo para pruebas: devuelve la consulta tal como la enlazó MVC.</summary>
[ApiController]
[Route("test-paging")]
public sealed class PagingTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] PagedQuery query) => Ok(query);
}

public sealed class PagingBindingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task QueryString_BindsAllParameters()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>(
            "/test-paging?page=3&pageSize=5&sortBy=price&sortDirection=desc");

        Assert.Equal(3, body.GetProperty("page").GetInt32());
        Assert.Equal(5, body.GetProperty("pageSize").GetInt32());
        Assert.Equal("price", body.GetProperty("sortBy").GetString());
        Assert.Equal("Desc", body.GetProperty("sortDirection").GetString());
    }

    [Fact]
    public async Task EmptyQueryString_UsesDefaults()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/test-paging");

        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(20, body.GetProperty("pageSize").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("sortDirection").ValueKind);
    }

    [Fact]
    public async Task InvalidSortDirection_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/test-paging?sortDirection=sideways");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
