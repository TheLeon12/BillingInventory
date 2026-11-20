using BillingInventory.Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace BillingInventory.Api.Authorization;

/// <summary>
/// Nombres de las políticas de autorización. Los endpoints se protegen por política (capacidad),
/// no por rol directamente, para cambiar quién puede hacer qué en un solo lugar.
/// </summary>
public static class Policies
{
    /// <summary>Solo administradores: gestión de usuarios y roles.</summary>
    public const string AdminOnly = nameof(AdminOnly);

    /// <summary>Gestionar clientes (Admin, Vendedor).</summary>
    public const string ManageCustomers = nameof(ManageCustomers);

    /// <summary>Crear y editar productos y categorías (Admin, Almacén).</summary>
    public const string ManageProducts = nameof(ManageProducts);

    /// <summary>Registrar movimientos de inventario (Admin, Almacén).</summary>
    public const string ManageInventory = nameof(ManageInventory);

    /// <summary>Emitir y anular facturas (Admin, Vendedor).</summary>
    public const string ManageInvoices = nameof(ManageInvoices);

    /// <summary>Consultar reportes de ventas (Admin, Vendedor).</summary>
    public const string ViewReports = nameof(ViewReports);

    /// <summary>
    /// Registra las políticas. Además define una política por defecto: todo endpoint exige un usuario
    /// autenticado salvo que se marque explícitamente con <c>[AllowAnonymous]</c>, de modo que olvidar
    /// un atributo nunca deja un endpoint abierto.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AdminOnly, p => p.RequireRole(RoleNames.Admin))
            .AddPolicy(ManageCustomers, p => p.RequireRole(RoleNames.Admin, RoleNames.Seller))
            .AddPolicy(ManageProducts, p => p.RequireRole(RoleNames.Admin, RoleNames.Warehouse))
            .AddPolicy(ManageInventory, p => p.RequireRole(RoleNames.Admin, RoleNames.Warehouse))
            .AddPolicy(ManageInvoices, p => p.RequireRole(RoleNames.Admin, RoleNames.Seller))
            .AddPolicy(ViewReports, p => p.RequireRole(RoleNames.Admin, RoleNames.Seller));

        return services;
    }
}
