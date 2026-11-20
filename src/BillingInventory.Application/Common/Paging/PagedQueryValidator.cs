using FluentValidation;

namespace BillingInventory.Application.Common.Paging;

/// <summary>
/// Reglas de validación de paginación. Los validadores de listados concretos heredan de esta clase
/// para reutilizarlas.
/// </summary>
/// <typeparam name="T">Tipo de consulta paginada.</typeparam>
public abstract class PagedQueryValidator<T> : AbstractValidator<T>
    where T : PagedQuery
{
    /// <summary>Crea el validador con las reglas de <see cref="PagedQuery"/>.</summary>
    protected PagedQueryValidator()
    {
        RuleFor(q => q.Page)
            .InclusiveBetween(1, PagedQuery.MaxPage)
            .WithMessage($"La página debe estar entre 1 y {PagedQuery.MaxPage}.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, PagedQuery.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {PagedQuery.MaxPageSize}.");
    }
}
