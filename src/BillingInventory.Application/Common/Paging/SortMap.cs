using System.Linq.Expressions;
using BillingInventory.Domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace BillingInventory.Application.Common.Paging;

/// <summary>
/// Lista blanca de campos ordenables de una entidad. Evita ordenar por columnas arbitrarias
/// que envíe el cliente y garantiza un orden estable entre páginas.
/// </summary>
/// <typeparam name="T">Entidad a ordenar.</typeparam>
public sealed class SortMap<T>
    where T : BaseEntity
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _orderings =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _defaultField;
    private readonly SortDirection _defaultDirection;

    /// <summary>Crea el mapa indicando el orden por defecto.</summary>
    /// <param name="defaultField">Nombre del campo usado cuando el cliente no indica <c>sortBy</c>. Debe registrarse con <see cref="Add{TKey}"/>.</param>
    /// <param name="defaultDirection">Dirección usada cuando el cliente no indica <c>sortDirection</c>.</param>
    public SortMap(string defaultField, SortDirection defaultDirection = SortDirection.Asc)
    {
        _defaultField = defaultField;
        _defaultDirection = defaultDirection;
    }

    /// <summary>Nombres de los campos permitidos.</summary>
    public IReadOnlyCollection<string> Fields => _orderings.Keys;

    /// <summary>Registra un campo ordenable.</summary>
    /// <typeparam name="TKey">Tipo del campo.</typeparam>
    /// <param name="name">Nombre público del campo (no distingue mayúsculas).</param>
    /// <param name="key">Expresión que selecciona el campo de la entidad.</param>
    /// <returns>El mismo mapa, para encadenar llamadas.</returns>
    public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        _orderings[name] = (query, descending) => descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return this;
    }

    /// <summary>Aplica el ordenamiento solicitado a la consulta.</summary>
    /// <param name="query">Consulta a ordenar.</param>
    /// <param name="sortBy">Campo solicitado; <c>null</c> usa el predeterminado.</param>
    /// <param name="direction">Dirección solicitada; <c>null</c> usa la predeterminada.</param>
    /// <returns>La consulta ordenada, con desempate por <c>Id</c> para que las páginas no se solapen.</returns>
    /// <exception cref="ValidationException"><paramref name="sortBy"/> no es un campo permitido.</exception>
    public IOrderedQueryable<T> Apply(IQueryable<T> query, string? sortBy, SortDirection? direction)
    {
        var field = string.IsNullOrWhiteSpace(sortBy) ? _defaultField : sortBy.Trim();

        if (!_orderings.TryGetValue(field, out var order))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    "sortBy",
                    $"Campo de orden no válido: '{field}'. Valores permitidos: {string.Join(", ", _orderings.Keys)}."),
            ]);
        }

        var descending = (direction ?? _defaultDirection) == SortDirection.Desc;

        // Sin desempate, filas con el mismo valor pueden repetirse u omitirse entre páginas.
        return order(query, descending).ThenBy(e => e.Id);
    }
}
