using System.Globalization;
using System.Security.Claims;

namespace BillingInventory.Api.Extensions;

/// <summary>Atajos para leer los claims del usuario autenticado.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Obtiene el identificador del usuario (claim <c>sub</c>) del token.</summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <returns>El identificador del usuario.</returns>
    /// <exception cref="InvalidOperationException">El token no contiene un identificador válido.</exception>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("sub"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidOperationException("El token no contiene un identificador de usuario válido.");
}
