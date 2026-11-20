using BillingInventory.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using PasswordVerificationResult = BillingInventory.Application.Abstractions.PasswordVerificationResult;

namespace BillingInventory.Infrastructure.Security;

/// <summary>
/// Hash de contraseñas con <see cref="PasswordHasher{TUser}"/> de ASP.NET Core Identity
/// (PBKDF2-HMAC-SHA512, sal aleatoria por contraseña e iteraciones configurables).
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // El hasher de Identity exige un objeto de usuario, pero no lo usa para calcular el hash.
    private static readonly object NoUser = new();

    private readonly PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    /// <inheritdoc />
    public PasswordVerificationResult Verify(string hash, string password)
    {
        // Un hash vacío o corrupto nunca debe lanzar: se trata como contraseña incorrecta.
        if (string.IsNullOrEmpty(hash))
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            return _hasher.VerifyHashedPassword(NoUser, hash, password) switch
            {
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success => PasswordVerificationResult.Success,
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
                _ => PasswordVerificationResult.Failed,
            };
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
