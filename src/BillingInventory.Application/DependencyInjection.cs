using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BillingInventory.Application;

/// <summary>Registro de los servicios de la capa Application.</summary>
public static class DependencyInjection
{
    /// <summary>Registra los validadores de FluentValidation definidos en Application.</summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
