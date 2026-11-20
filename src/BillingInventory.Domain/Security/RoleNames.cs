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
}
