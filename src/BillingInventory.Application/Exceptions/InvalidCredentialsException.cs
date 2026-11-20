namespace BillingInventory.Application.Exceptions;

/// <summary>
/// Se lanza cuando el correo o la contraseña no son correctos, o el usuario está inactivo.
/// El mensaje es siempre el mismo para no revelar si el correo existe. Se traduce a HTTP 401.
/// </summary>
public sealed class InvalidCredentialsException() : Exception("Correo o contraseña incorrectos.");
