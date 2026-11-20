using BillingInventory.Application.Common.Paging;

namespace BillingInventory.Application.Users;

/// <summary>Filtros del listado de usuarios, además de la paginación y el orden comunes.</summary>
public sealed record UserQuery : PagedQuery
{
    /// <summary>Texto a buscar en el correo o el nombre.</summary>
    /// <example>maria</example>
    public string? Search { get; init; }

    /// <summary>Filtra por nombre de rol: <c>Admin</c>, <c>Vendedor</c> o <c>Almacen</c>.</summary>
    /// <example>Vendedor</example>
    public string? Role { get; init; }

    /// <summary>Filtra por usuarios activos (<c>true</c>) o inactivos (<c>false</c>).</summary>
    /// <example>true</example>
    public bool? IsActive { get; init; }
}

/// <summary>Datos de un usuario para administración.</summary>
public sealed record UserAdminResponse
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

    /// <summary>Indica si el usuario puede iniciar sesión.</summary>
    /// <example>true</example>
    public bool IsActive { get; init; }

    /// <summary>Fecha de creación en UTC.</summary>
    /// <example>2026-11-20T15:30:00Z</example>
    public DateTime CreatedAtUtc { get; init; }
}

/// <summary>Solicitud para cambiar el rol de un usuario.</summary>
public sealed record ChangeRoleRequest
{
    /// <summary>Nuevo rol: <c>Admin</c>, <c>Vendedor</c> o <c>Almacen</c> (sin distinguir mayúsculas).</summary>
    /// <example>Almacen</example>
    public string Role { get; init; } = string.Empty;
}

/// <summary>Solicitud para activar o desactivar un usuario.</summary>
public sealed record SetUserStatusRequest
{
    /// <summary><c>true</c> para activar, <c>false</c> para desactivar.</summary>
    /// <example>false</example>
    public bool IsActive { get; init; }
}
