using BillingInventory.Application.Abstractions;
using BillingInventory.Application.Exceptions;
using BillingInventory.Domain.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingInventory.Application.Auth;

/// <summary>Implementación de <see cref="IAuthService"/>.</summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    ILogger<AuthService> logger) : IAuthService
{
    // Hash de relleno para que el login tarde lo mismo exista o no el correo (evita enumerar usuarios por tiempo).
    private static string? _dummyHash;

    /// <inheritdoc />
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException("Ya existe un usuario con ese correo.");
        }

        // El rol nunca viene del cliente: el registro público siempre crea un Vendedor.
        var sellerRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Seller, cancellationToken);

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            RoleId = sellerRole.Id,
        };
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo correo: el índice único dejó pasar solo uno.
            if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != user.Id, cancellationToken))
            {
                throw new ConflictException("Ya existe un usuario con ese correo.");
            }

            throw;
        }

        var response = ToResponse(user, sellerRole.Name);
        logger.LogInformation("Usuario registrado {UserId} con rol {Role}", response.Id, response.Role);

        return response;
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            _dummyHash ??= passwordHasher.Hash("relleno-para-igualar-tiempos");
            passwordHasher.Verify(_dummyHash, request.Password);
            logger.LogWarning("Login fallido: correo no registrado");
            throw new InvalidCredentialsException();
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed || !user.IsActive)
        {
            logger.LogWarning("Login fallido para el usuario {UserId} (activo: {IsActive})", user.Id, user.IsActive);
            throw new InvalidCredentialsException();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var (token, expiresAtUtc) = tokenGenerator.Generate(user);
        var userResponse = ToResponse(user, user.Role.Name);
        logger.LogInformation("Login correcto del usuario {UserId}", userResponse.Id);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            User = userResponse,
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(User user, string roleName) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = roleName,
    };
}
