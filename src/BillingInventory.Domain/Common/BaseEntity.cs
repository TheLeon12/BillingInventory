namespace BillingInventory.Domain.Common;

/// <summary>
/// Clase base de todas las entidades persistentes. Aporta identidad, campos de auditoría
/// y un token de concurrencia optimista.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Identificador único (clave primaria autoincremental).</summary>
    public int Id { get; set; }

    /// <summary>Fecha y hora UTC de creación. La asigna el contexto al guardar.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Fecha y hora UTC de la última modificación; <c>null</c> si nunca se modificó.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Token de concurrencia optimista (<c>rowversion</c> de SQL Server). Si dos usuarios
    /// modifican la misma fila, el segundo guardado falla con <c>DbUpdateConcurrencyException</c>.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
