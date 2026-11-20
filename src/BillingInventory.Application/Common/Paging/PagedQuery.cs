namespace BillingInventory.Application.Common.Paging;

/// <summary>
/// Parámetros de paginación y ordenamiento comunes a todos los listados.
/// Los listados concretos heredan de este tipo para agregar sus propios filtros.
/// </summary>
public record PagedQuery
{
    /// <summary>Tamaño de página máximo permitido.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Número máximo de página; evita desbordes al calcular el desplazamiento.</summary>
    public const int MaxPage = 1_000_000;

    /// <summary>Número de página, comenzando en 1.</summary>
    /// <example>1</example>
    public int Page { get; init; } = 1;

    /// <summary>Cantidad de elementos por página (1 a 100).</summary>
    /// <example>20</example>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Campo por el que se ordena. Cada listado define sus campos permitidos;
    /// si se omite se usa el orden por defecto del listado.
    /// </summary>
    /// <example>name</example>
    public string? SortBy { get; init; }

    /// <summary>Dirección del orden (<c>asc</c> o <c>desc</c>). Si se omite se usa la del listado.</summary>
    /// <example>asc</example>
    public SortDirection? SortDirection { get; init; }
}
