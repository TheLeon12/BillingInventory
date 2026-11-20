using BillingInventory.Domain.Security;

namespace BillingInventory.Application.Abstractions;

/// <summary>Genera tokens JWT de acceso.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Crea un token firmado para el usuario, con su identificador, correo, nombre y rol.</summary>
    /// <param name="user">Usuario autenticado; su propiedad <see cref="User.Role"/> debe estar cargada.</param>
    /// <returns>El token y su fecha de expiración en UTC.</returns>
    (string Token, DateTime ExpiresAtUtc) Generate(User user);
}
