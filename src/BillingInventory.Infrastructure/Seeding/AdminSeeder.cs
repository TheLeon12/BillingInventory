using BillingInventory.Application.Abstractions;
using BillingInventory.Application.Auth;
using BillingInventory.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingInventory.Infrastructure.Seeding;

/// <summary>Resultado de intentar crear el administrador inicial.</summary>
public enum AdminSeedResult
{
    /// <summary>No hay datos de administrador en la configuración; no se hizo nada.</summary>
    NotConfigured,

    /// <summary>Se creó el administrador inicial.</summary>
    Created,

    /// <summary>Ya existe al menos un administrador; no se hizo nada.</summary>
    AdminAlreadyExists,

    /// <summary>El correo configurado ya pertenece a un usuario que no es administrador; no se modificó.</summary>
    EmailInUse,
}

/// <summary>Se lanza cuando la configuración <c>Seed</c> es incompleta o inválida.</summary>
/// <param name="message">Descripción del problema; nunca incluye la contraseña.</param>
public sealed class SeedConfigurationException(string message) : Exception(message);

/// <summary>
/// Crea el primer administrador cuando el sistema no tiene ninguno. Es idempotente y nunca modifica
/// usuarios existentes: no resetea contraseñas ni asciende cuentas.
/// </summary>
public sealed class AdminSeeder(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<AdminSeeder> logger)
{
    /// <summary>Ejecuta el sembrado según la configuración.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Qué ocurrió.</returns>
    /// <exception cref="SeedConfigurationException">
    /// Solo se configuró uno de los dos campos, o el correo o la contraseña no cumplen las reglas del registro.
    /// </exception>
    public async Task<AdminSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var hasEmail = !string.IsNullOrWhiteSpace(settings.AdminEmail);
        var hasPassword = !string.IsNullOrWhiteSpace(settings.AdminPassword);

        // Sin configuración no se toca la base de datos: la API arranca igual aunque no esté disponible.
        if (!hasEmail && !hasPassword)
        {
            logger.LogInformation("Seed:AdminEmail y Seed:AdminPassword no están configurados; no se crea administrador inicial.");
            return AdminSeedResult.NotConfigured;
        }

        if (hasEmail != hasPassword)
        {
            throw new SeedConfigurationException("Seed:AdminEmail y Seed:AdminPassword deben configurarse juntos.");
        }

        var request = new RegisterRequest
        {
            Email = settings.AdminEmail!,
            FullName = settings.AdminFullName,
            Password = settings.AdminPassword!,
        };

        var validation = await new RegisterRequestValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new SeedConfigurationException(
                "La configuración Seed del administrador no es válida: "
                + string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        if (await db.Users.AnyAsync(u => u.Role.Name == RoleNames.Admin, cancellationToken))
        {
            logger.LogInformation("Ya existe un administrador; el sembrado inicial no hace nada.");
            return AdminSeedResult.AdminAlreadyExists;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // Nunca se asciende una cuenta existente: si alguien registró ese correo antes de tiempo,
        // ascenderla daría control de administrador a quien conoce esa contraseña.
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            logger.LogWarning(
                "El correo de Seed:AdminEmail ya pertenece a un usuario que no es administrador. " +
                "No se modificó. Use otro correo o asigne el rol con PUT /api/users/{{id}}/role desde un administrador.");
            return AdminSeedResult.EmailInUse;
        }

        var adminRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Admin, cancellationToken);
        db.Users.Add(new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            RoleId = adminRole.Id,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Otra instancia arrancó a la vez y creó el administrador antes: el resultado es el mismo.
            if (await db.Users.AsNoTracking().AnyAsync(u => u.Role.Name == RoleNames.Admin, cancellationToken))
            {
                return AdminSeedResult.AdminAlreadyExists;
            }

            throw;
        }

        logger.LogInformation("Administrador inicial creado ({Email}).", email);
        return AdminSeedResult.Created;
    }
}
