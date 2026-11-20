using FluentValidation;
using BillingInventory.Application.Exceptions;

namespace BillingInventory.Application.Auth;

/// <summary>Registro e inicio de sesión de usuarios.</summary>
public interface IAuthService
{
    /// <summary>Registra un usuario nuevo con el rol Vendedor.</summary>
    /// <param name="request">Datos del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario creado.</returns>
    /// <exception cref="ValidationException">Los datos no cumplen las reglas de validación.</exception>
    /// <exception cref="ConflictException">Ya existe un usuario con ese correo.</exception>
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Autentica a un usuario y emite un token JWT.</summary>
    /// <param name="request">Credenciales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El token y los datos del usuario.</returns>
    /// <exception cref="ValidationException">Faltan campos o tienen formato inválido.</exception>
    /// <exception cref="InvalidCredentialsException">Correo o contraseña incorrectos, o usuario inactivo.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
