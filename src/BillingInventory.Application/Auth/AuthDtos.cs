namespace BillingInventory.Application.Auth;

/// <summary>Datos para registrar un usuario nuevo. El rol asignado siempre es Vendedor.</summary>
public sealed record RegisterRequest
{
    /// <summary>Correo electrónico; será el nombre de usuario.</summary>
    /// <example>maria.perez@ejemplo.com</example>
    public string Email { get; init; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    /// <example>María Pérez</example>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Contraseña: 8 a 128 caracteres con mayúscula, minúscula y número.</summary>
    /// <example>Clave#2026</example>
    public string Password { get; init; } = string.Empty;
}

/// <summary>Credenciales de inicio de sesión.</summary>
public sealed record LoginRequest
{
    /// <summary>Correo electrónico registrado.</summary>
    /// <example>maria.perez@ejemplo.com</example>
    public string Email { get; init; } = string.Empty;

    /// <summary>Contraseña.</summary>
    /// <example>Clave#2026</example>
    public string Password { get; init; } = string.Empty;
}

/// <summary>Datos públicos de un usuario. Nunca incluye el hash de la contraseña.</summary>
public sealed record UserResponse
{
    /// <summary>Identificador del usuario.</summary>
    /// <example>7</example>
    public int Id { get; init; }

    /// <summary>Correo electrónico.</summary>
    /// <example>maria.perez@ejemplo.com</example>
    public string Email { get; init; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    /// <example>María Pérez</example>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Nombre del rol asignado.</summary>
    /// <example>Vendedor</example>
    public string Role { get; init; } = string.Empty;
}

/// <summary>Respuesta de un inicio de sesión correcto.</summary>
public sealed record LoginResponse
{
    /// <summary>Token JWT que se envía en la cabecera <c>Authorization: Bearer {token}</c>.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Tipo de token.</summary>
    /// <example>Bearer</example>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Fecha y hora UTC en que el token deja de ser válido.</summary>
    /// <example>2026-11-20T15:30:00Z</example>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>Usuario autenticado.</summary>
    public UserResponse User { get; init; } = new();
}
