namespace BillingInventory.Application.Exceptions;

/// <summary>
/// Se lanza cuando la operación choca con el estado actual (por ejemplo, SKU duplicado).
/// Se traduce a HTTP 409.
/// </summary>
/// <param name="message">Mensaje para el cliente de la API.</param>
public sealed class ConflictException(string message) : Exception(message);
