namespace BillingInventory.Domain.Security;

/// <summary>
/// Nombres de los roles del sistema. Se usan como valor del claim de rol en el JWT
/// y en las políticas de autorización, por eso no llevan acentos.
/// </summary>
public static class RoleNames
{
    /// <summary>Administrador: acceso total, incluida la gestión de usuarios y roles.</summary>
    public const string Admin = "Admin";

    /// <summary>Vendedor: gestiona clientes y emite facturas.</summary>
    public const string Seller = "Vendedor";

    /// <summary>Almacén: gestiona productos e inventario.</summary>
    public const string Warehouse = "Almacen";

    /// <summary>Todos los roles válidos del sistema.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Seller, Warehouse];

    /// <summary>Devuelve el nombre canónico de un rol ignorando mayúsculas, o <c>null</c> si no existe.</summary>
    /// <param name="name">Nombre recibido.</param>
    /// <returns>El nombre canónico (por ejemplo, <c>"Admin"</c> para <c>"admin"</c>) o <c>null</c>.</returns>
    public static string? Normalize(string? name) =>
        All.FirstOrDefault(r => string.Equals(r, name?.Trim(), StringComparison.OrdinalIgnoreCase));
}
