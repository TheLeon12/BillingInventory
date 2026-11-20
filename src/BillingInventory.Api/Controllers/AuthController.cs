using System.Security.Claims;
using BillingInventory.Api.Extensions;
using BillingInventory.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillingInventory.Api.Controllers;

/// <summary>Registro, inicio de sesión y consulta del usuario autenticado.</summary>
/// <param name="authService">Servicio de autenticación.</param>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Registra un usuario nuevo.</summary>
    /// <remarks>
    /// El usuario siempre se crea con el rol <c>Vendedor</c>. Un administrador puede cambiar el rol después.
    ///
    /// Ejemplo de solicitud:
    ///
    ///     POST /api/auth/register
    ///     {
    ///       "email": "maria.perez@ejemplo.com",
    ///       "fullName": "María Pérez",
    ///       "password": "Clave#2026"
    ///     }
    /// </remarks>
    /// <param name="request">Datos del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario creado.</returns>
    /// <response code="201">Usuario creado.</response>
    /// <response code="400">Datos inválidos (correo mal formado, contraseña débil...).</response>
    /// <response code="409">Ya existe un usuario con ese correo.</response>
    /// <response code="429">Demasiados intentos desde la misma IP.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    /// <remarks>
    /// Use el token en la cabecera <c>Authorization: Bearer {token}</c>, o con el botón *Authorize* de Swagger.
    ///
    /// Ejemplo de solicitud:
    ///
    ///     POST /api/auth/login
    ///     {
    ///       "email": "maria.perez@ejemplo.com",
    ///       "password": "Clave#2026"
    ///     }
    /// </remarks>
    /// <param name="request">Credenciales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El token de acceso y los datos del usuario.</returns>
    /// <response code="200">Inicio de sesión correcto.</response>
    /// <response code="400">Faltan campos obligatorios.</response>
    /// <response code="401">Correo o contraseña incorrectos, o usuario inactivo.</response>
    /// <response code="429">Demasiados intentos desde la misma IP.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>Devuelve los datos del usuario autenticado según su token.</summary>
    /// <returns>Identificador, correo, nombre y rol del token.</returns>
    /// <response code="200">Token válido.</response>
    /// <response code="401">Falta el token, es inválido o expiró.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new UserResponse
    {
        Id = int.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture),
        Email = User.FindFirstValue("email")!,
        FullName = User.FindFirstValue("name")!,
        Role = User.FindFirstValue("role")!,
    });
}
