namespace BillingInventory.Application.Exceptions;

/// <summary>
/// Se lanza cuando la petición es válida pero viola una regla de negocio
/// (por ejemplo, stock insuficiente). Se traduce a HTTP 422.
/// </summary>
/// <param name="message">Mensaje para el cliente de la API.</param>
public sealed class BusinessRuleException(string message) : Exception(message);
