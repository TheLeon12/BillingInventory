namespace BillingInventory.Application.Common.Paging;

/// <summary>Una página de resultados con sus metadatos.</summary>
/// <typeparam name="T">Tipo de los elementos (siempre un DTO, nunca una entidad).</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="Page">Número de página devuelta (desde 1).</param>
/// <param name="PageSize">Tamaño de página solicitado.</param>
/// <param name="TotalCount">Total de elementos que cumplen el filtro, en todas las páginas.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Total de páginas disponibles.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Indica si existe una página anterior.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>Indica si existe una página siguiente.</summary>
    public bool HasNext => Page < TotalPages;
}
