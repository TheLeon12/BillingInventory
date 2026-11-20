using System.ComponentModel.DataAnnotations;
using BillingInventory.Application.Auth;
using BillingInventory.Domain.Security;
using BillingInventory.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using System.Text;

namespace BillingInventory.UnitTests.Security;

public sealed class AuthUnitTests
{
    private const string Key = "clave-de-pruebas-unitarias-0123456789-abcdefghijklmnopqrstuvwxyz";

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "emisor",
        Audience = "audiencia",
        Key = Key,
        ExpirationMinutes = 30,
    };

    private static User SampleUser() => new()
    {
        Id = 7,
        Email = "maria@ejemplo.com",
        FullName = "María Pérez",
        Role = new Role { Id = 2, Name = RoleNames.Seller },
    };

    [Theory]
    [InlineData("Clave#2026", true)]
    [InlineData("clave#2026", false)] // sin mayúscula
    [InlineData("CLAVE#2026", false)] // sin minúscula
    [InlineData("Clave#abcd", false)] // sin número
    [InlineData("Cl#1", false)] // muy corta
    [InlineData("", false)]
    public void RegisterValidator_EnforcesPasswordPolicy(string password, bool valid)
    {
        var request = new RegisterRequest { Email = "a@b.com", FullName = "Ana", Password = password };

        new RegisterRequestValidator().Validate(request).IsValid.ShouldBe(valid);
    }

    [Fact]
    public void RegisterValidator_RejectsHugePasswords()
    {
        var request = new RegisterRequest { Email = "a@b.com", FullName = "Ana", Password = "Aa1" + new string('x', 200) };

        new RegisterRequestValidator().Validate(request).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("no-es-un-correo")]
    [InlineData("")]
    [InlineData("a@")]
    public void RegisterValidator_RejectsInvalidEmails(string email)
    {
        var request = new RegisterRequest { Email = email, FullName = "Ana", Password = "Clave#2026" };

        new RegisterRequestValidator().Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void LoginValidator_DoesNotEnforcePasswordComplexity()
    {
        // En el login no se revelan las reglas de contraseña: solo se exige que venga algo.
        var request = new LoginRequest { Email = "a@b.com", Password = "x" };

        new LoginRequestValidator().Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task JwtGenerator_EmitsSignedTokenWithExpectedClaims()
    {
        var now = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var generator = new JwtTokenGenerator(Options.Create(ValidOptions()), new FixedTimeProvider(now));

        var (token, expires) = generator.Generate(SampleUser());

        expires.ShouldBe(new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Utc));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "emisor",
            ValidAudience = "audiencia",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidateLifetime = false, // la hora de la prueba es fija y ya pasó
        });

        result.IsValid.ShouldBeTrue();
        var jwt = (JsonWebToken)result.SecurityToken;
        jwt.Subject.ShouldBe("7");
        jwt.GetClaim("email").Value.ShouldBe("maria@ejemplo.com");
        jwt.GetClaim("role").Value.ShouldBe("Vendedor");
        jwt.GetClaim("name").Value.ShouldBe("María Pérez");
        jwt.Claims.ShouldNotContain(c => c.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void JwtGenerator_EachTokenHasUniqueJti()
    {
        var generator = new JwtTokenGenerator(Options.Create(ValidOptions()), TimeProvider.System);
        var handler = new JsonWebTokenHandler();

        var first = handler.ReadJsonWebToken(generator.Generate(SampleUser()).Token).Id;
        var second = handler.ReadJsonWebToken(generator.Generate(SampleUser()).Token).Id;

        first.ShouldNotBe(second);
    }

    [Fact]
    public void JwtOptions_ShortKey_FailsValidation()
    {
        var options = ValidOptions();
        options.Key = "demasiado-corta";

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        valid.ShouldBeFalse();
        results.ShouldContain(r => r.ErrorMessage!.Contains("32 bytes", StringComparison.Ordinal));
    }

    [Fact]
    public void JwtOptions_ValidConfiguration_Passes()
    {
        var options = ValidOptions();

        Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true)
            .ShouldBeTrue();
    }
}
