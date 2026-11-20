using BillingInventory.Application.Abstractions;
using BillingInventory.Infrastructure.Security;
using Shouldly;

namespace BillingInventory.UnitTests.Security;

public sealed class PasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_NeverContainsThePlainPassword()
    {
        var hash = _hasher.Hash("Secreta#2026");

        hash.ShouldNotContain("Secreta#2026");
        hash.Length.ShouldBeLessThan(500); // cabe en la columna PasswordHash
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Sal aleatoria: dos usuarios con la misma contraseña no comparten hash.
        _hasher.Hash("Secreta#2026").ShouldNotBe(_hasher.Hash("Secreta#2026"));
    }

    [Fact]
    public void Verify_CorrectPassword_Succeeds()
    {
        var hash = _hasher.Hash("Secreta#2026");

        _hasher.Verify(hash, "Secreta#2026").ShouldBe(PasswordVerificationResult.Success);
    }

    [Theory]
    [InlineData("secreta#2026")]
    [InlineData("Secreta#2027")]
    [InlineData("")]
    public void Verify_WrongPassword_Fails(string attempt)
    {
        var hash = _hasher.Hash("Secreta#2026");

        _hasher.Verify(hash, attempt).ShouldBe(PasswordVerificationResult.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-hash")]
    [InlineData("%%%%")]
    public void Verify_EmptyOrCorruptHash_FailsWithoutThrowing(string hash)
    {
        _hasher.Verify(hash, "Secreta#2026").ShouldBe(PasswordVerificationResult.Failed);
    }
}
