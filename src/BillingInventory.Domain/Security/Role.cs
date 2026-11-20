using BillingInventory.Domain.Common;

namespace BillingInventory.Domain.Security;

/// <summary>Rol de un usuario. Los tres roles del sistema se crean con la migración (ver <see cref="RoleNames"/>).</summary>
public class Role : BaseEntity
{
    /// <summary>Nombre único del rol.</summary>
    public string Name { get; set; } = string.Empty;
}
