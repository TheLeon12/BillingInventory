using System.Text;
using BillingInventory.Application.Abstractions;
using BillingInventory.Domain.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BillingInventory.Infrastructure.Security;

/// <summary>Genera tokens JWT firmados con HMAC-SHA256.</summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    /// <summary>Nombre del claim que contiene el rol.</summary>
    public const string RoleClaim = "role";

    /// <summary>Nombre del claim que contiene el nombre completo.</summary>
    public const string NameClaim = "name";

    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>Crea el generador.</summary>
    /// <param name="options">Configuración JWT ya validada.</param>
    /// <param name="clock">Reloj, para poder fijar la hora en pruebas.</param>
    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public (string Token, DateTime ExpiresAtUtc) Generate(User user)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [NameClaim] = user.FullName,
                [RoleClaim] = user.Role.Name,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        return (_handler.CreateToken(descriptor), expires);
    }
}
