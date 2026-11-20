using FluentValidation;

namespace BillingInventory.Application.Auth;

/// <summary>Reglas de validación del registro.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Longitud máxima de contraseña; evita hashear textos enormes (denegación de servicio).</summary>
    public const int MaxPasswordLength = 128;

    /// <summary>Define las reglas.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(256).WithMessage("El correo no puede superar 256 caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(MaxPasswordLength).WithMessage($"La contraseña no puede superar {MaxPasswordLength} caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe incluir una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe incluir una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir un número.");
    }
}

/// <summary>Reglas de validación del inicio de sesión (solo formato; no revela las reglas de contraseña).</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Define las reglas.</summary>
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(256).WithMessage("El correo no puede superar 256 caracteres.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(RegisterRequestValidator.MaxPasswordLength)
            .WithMessage($"La contraseña no puede superar {RegisterRequestValidator.MaxPasswordLength} caracteres.");
    }
}
