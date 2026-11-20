using BillingInventory.Domain.Common;

namespace BillingInventory.Domain.Security;

/// <summary>Usuario que puede autenticarse en la API. Tiene exactamente un rol.</summary>
public class User : BaseEntity
{
    /// <summary>Correo electrónico en minúsculas; es el nombre de usuario y es único.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo para mostrar.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña. Nunca se guarda ni se devuelve la contraseña en texto plano.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Si es <c>false</c>, el usuario no puede iniciar sesión.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Identificador del rol asignado.</summary>
    public int RoleId { get; set; }

    /// <summary>Rol asignado.</summary>
    public Role Role { get; set; } = null!;
}
