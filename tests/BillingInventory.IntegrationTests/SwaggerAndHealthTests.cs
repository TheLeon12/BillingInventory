using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BillingInventory.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class SwaggerAndHealthTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Swagger_InDevelopment_PublishesDocumentWithBearerScheme()
    {
        await using var factory = new ApiFactory().WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("API de Facturación e Inventario", doc.GetProperty("info").GetProperty("title").GetString());
        var bearer = doc.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task SwaggerUi_InDevelopment_IsServed()
    {
        await using var factory = new ApiFactory().WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_InProduction_IsNotPublished()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsHealthyWithoutTouchingDatabase()
    {
        // La cadena por defecto apunta a una base inexistente: /health no debe depender de ella.
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_WithDatabaseUp_ReturnsHealthy()
    {
        await using var factory = new ApiFactory { ConnectionString = sql.ConnectionString };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("checks").GetProperty("database").GetString());
    }

    [Fact]
    public async Task Ready_WithDatabaseDown_Returns503WithoutLeakingDetails()
    {
        await using var factory = new ApiFactory
        {
            ConnectionString = "Server=localhost,1;Database=x;User Id=sa;Password=NoImporta1!;Connect Timeout=2;TrustServerCertificate=True",
        };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Unhealthy", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NoImporta1!", text, StringComparison.Ordinal);
    }
}
