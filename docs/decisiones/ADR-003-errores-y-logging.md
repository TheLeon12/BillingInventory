# ADR-003: Manejo centralizado de errores y logging estructurado

- **Estado:** Aceptada
- **Fecha:** 2026-11-19

## Contexto

La API debe responder a los errores de forma uniforme, sin filtrar detalles internos, y dejar trazas que permitan seguir una petición de punta a punta.

## Decisión

**Errores**
- Un único `IExceptionHandler` (`GlobalExceptionHandler`) con `AddProblemDetails()` produce respuestas `application/problem+json` (RFC 9457). `UseStatusCodePages()` hace lo mismo para 404/401/403 sin cuerpo.
- Los servicios de Application lanzan excepciones tipadas en lugar de devolver códigos:

| Excepción | HTTP |
|---|---|
| `FluentValidation.ValidationException` | 400 (con `errors` por campo) |
| `NotFoundException` | 404 |
| `ConflictException`, `DbUpdateConcurrencyException` | 409 |
| `BusinessRuleException` | 422 |
| cualquier otra | 500 (mensaje genérico) |

- Los 5xx nunca incluyen el mensaje de la excepción; solo se registran en el log.
- Toda respuesta de error lleva `traceId` y `correlationId`.

**Logging**
- Serilog configurado desde `appsettings` (`Serilog:*`): consola legible y archivo JSON compacto con rotación diaria en `logs/` (14 días).
- `CorrelationIdMiddleware` reutiliza `X-Correlation-Id` si es válido (`[A-Za-z0-9_-]{1,64}`, para evitar inyección en logs) o genera uno; lo devuelve en la respuesta y lo agrega a todos los logs mediante `LogContext`.
- `UseSerilogRequestLogging()` registra una línea por petición. No se registran cuerpos ni cabeceras, para no filtrar datos sensibles.
- Se usa un logger de arranque mínimo para capturar fallos previos a leer la configuración. `HostAbortedException` (herramientas de EF) no se trata como fallo.

## Consecuencias

- Los clientes reciben un formato de error único y predecible.
- El correlationId permite buscar en el JSON de logs todo lo ocurrido en una petición.
- Se desactiva CA1848 (`LoggerMessage`) para permitir plantillas simples en `ILogger`; Serilog ya procesa plantillas de forma eficiente para esta escala.
