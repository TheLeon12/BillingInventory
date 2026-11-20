using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.Application.Common.Paging;

/// <summary>Extensiones de <see cref="IQueryable{T}"/> para filtros opcionales y paginación.</summary>
public static class QueryableExtensions
{
    /// <summary>Aplica el filtro solo si la condición se cumple (útil para filtros opcionales).</summary>
    /// <typeparam name="T">Tipo de elemento.</typeparam>
    /// <param name="query">Consulta original.</param>
    /// <param name="condition">Si es <c>false</c>, la consulta se devuelve sin cambios.</param>
    /// <param name="predicate">Filtro a aplicar.</param>
    /// <returns>La consulta filtrada o la original.</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;

    /// <summary>
    /// Ejecuta la consulta y devuelve solo la página pedida, con el total de elementos.
    /// La consulta debe llegar ya ordenada para que el resultado sea determinista.
    /// </summary>
    /// <typeparam name="T">Tipo de elemento (normalmente un DTO ya proyectado).</typeparam>
    /// <param name="query">Consulta filtrada y ordenada.</param>
    /// <param name="page">Número de página desde 1.</param>
    /// <param name="pageSize">Elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La página con sus metadatos.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);

        // Si no hay filas se evita la segunda consulta.
        IReadOnlyList<T> items = total == 0
            ? []
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, total);
    }
}
