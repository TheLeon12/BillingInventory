namespace BillingInventory.Infrastructure.Seeding;

/// <summary>
/// Datos del administrador inicial (sección <c>Seed</c>). Deben venir de user-secrets o variables de
/// entorno: la contraseña nunca va en archivos del repositorio.
/// </summary>
public sealed class SeedOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Seed";

    /// <summary>Correo del administrador inicial. Si no se configura, no se crea ninguno.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>Contraseña del administrador inicial. Debe cumplir la misma política que el registro.</summary>
    public string? AdminPassword { get; set; }

    /// <summary>Nombre para mostrar del administrador inicial.</summary>
    public string AdminFullName { get; set; } = "Administrador";
}
