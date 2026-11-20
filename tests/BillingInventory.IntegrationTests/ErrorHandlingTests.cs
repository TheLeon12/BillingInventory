using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BillingInventory.IntegrationTests;

public sealed class ErrorHandlingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound)]
    [InlineData("conflict", HttpStatusCode.Conflict)]
    [InlineData("rule", HttpStatusCode.UnprocessableEntity)]
    [InlineData("concurrency", HttpStatusCode.Conflict)]
    [InlineData("validation", HttpStatusCode.BadRequest)]
    [InlineData("crash", HttpStatusCode.InternalServerError)]
    public async Task Exceptions_AreMappedToProblemDetails(string route, HttpStatusCode expected)
    {
        var response = await _client.GetAsync($"/test-errors/{route}");

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("title", out _));
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.Equal(
            response.Headers.GetValues("X-Correlation-Id").Single(),
            body.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task ValidationError_ListsErrorsPerField()
    {
        var response = await _client.GetAsync("/test-errors/validation");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var errors = body.GetProperty("errors");
        Assert.Equal("El nombre es obligatorio.", errors.GetProperty("Name")[0].GetString());
        Assert.Equal("El precio debe ser mayor que cero.", errors.GetProperty("Price")[0].GetString());
    }

    [Theory]
    [InlineData("crash", "detalle interno")]
    [InlineData("concurrency", "secreto interno")]
    public async Task InternalMessages_AreNeverExposed(string route, string secret)
    {
        var response = await _client.GetAsync($"/test-errors/{route}");
        var text = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownRoute_Authenticated_ReturnsProblemDetails404()
    {
        var request = Security.TestTokens.Get("/does-not-exist", Security.TestTokens.For("Vendedor"));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownRoute_Anonymous_Returns401SoRoutesAreNotRevealed()
    {
        var response = await _client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CorrelationId_ValidIncomingValueIsReused()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-errors/conflict");
        request.Headers.Add("X-Correlation-Id", "abc-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("abc-123", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task CorrelationId_InvalidIncomingValueIsReplaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-errors/conflict");
        request.Headers.Add("X-Correlation-Id", "mal valor con espacios");

        var response = await _client.SendAsync(request);

        var id = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.NotEqual("mal valor con espacios", id);
        Assert.Matches("^[0-9a-f]{32}$", id);
    }
}
