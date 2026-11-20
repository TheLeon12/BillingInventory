using BillingInventory.Application.Common.Paging;
using BillingInventory.Domain.Security;
using FluentValidation;

namespace BillingInventory.Application.Users;

/// <summary>Reglas del listado de usuarios.</summary>
public sealed class UserQueryValidator : PagedQueryValidator<UserQuery>
{
    /// <summary>Define las reglas.</summary>
    public UserQueryValidator()
    {
        RuleFor(q => q.Search)
            .MaximumLength(100).WithMessage("La búsqueda no puede superar 100 caracteres.");

        RuleFor(q => q.Role)
            .Must(r => RoleNames.Normalize(r) is not null)
            .When(q => !string.IsNullOrWhiteSpace(q.Role))
            .WithMessage($"Rol no válido. Valores permitidos: {string.Join(", ", RoleNames.All)}.");
    }
}

/// <summary>Reglas del cambio de rol.</summary>
public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    /// <summary>Define las reglas.</summary>
    public ChangeRoleRequestValidator()
    {
        RuleFor(r => r.Role)
            .Must(r => RoleNames.Normalize(r) is not null)
            .WithMessage($"Rol no válido. Valores permitidos: {string.Join(", ", RoleNames.All)}.");
    }
}
