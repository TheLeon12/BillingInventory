using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BillingInventory.IntegrationTests.Security;

/// <summary>Crea tokens válidos para las pruebas sin pasar por el login (no requiere base de datos).</summary>
public static class TestTokens
{
    /// <summary>Genera un token firmado con la clave de pruebas de <see cref="ApiFactory"/>.</summary>
    public static string For(string role, int userId = 1)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ApiFactory.JwtIssuer,
            Audience = ApiFactory.JwtAudience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["email"] = $"{role.ToLowerInvariant()}@prueba.test",
                ["name"] = $"Usuario {role}",
                ["role"] = role,
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>Crea una petición GET con el token indicado (o sin token si es <c>null</c>).</summary>
    public static HttpRequestMessage Get(string url, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }
}
