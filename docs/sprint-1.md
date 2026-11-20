# Sprint 1: Base del proyecto

## Objetivo

Dejar lista la base técnica sobre la que se construyen los demás módulos: estructura en capas, persistencia con EF Core y SQL Server, manejo de errores, logging, documentación de la API y paginación compartida.

## Bloques completados

| Bloque | Contenido |
|---|---|
| 1.1 | Limpieza de la plantilla, `Directory.Build.props`, configuración de ejemplo, README, CHANGELOG y ADR-001 |
| 1.2 | `BaseEntity`, `AppDbContext`, `IAppDbContext`, migración `InitialCreate`, fixture de SQL Server con Testcontainers y ADR-002 |
| 1.3 | Serilog (consola y JSON), `CorrelationIdMiddleware`, `GlobalExceptionHandler` con `ProblemDetails`, excepciones tipadas y ADR-003 |
| 1.4 | Swagger/OpenAPI con comentarios XML y esquema Bearer, `/health` y `/health/ready` |
| 1.5 | Paginación, filtros y ordenamiento compartidos, enumeraciones como texto y ADR-004 |

## Resultado

- Compilación con 0 advertencias y 0 errores (warnings tratados como errores, XML docs obligatorias).
- 43 pruebas pasando: 15 unitarias y 28 de integración (SQL Server real en contenedor).

## Decisiones técnicas

- Arquitectura en capas sin repositorios genéricos: los servicios usan `IAppDbContext` (ADR-001).
- Code First, `RowVersion` en toda entidad, auditoría UTC con `TimeProvider` y `decimal(18,2)` global (ADR-002).
- Errores uniformes en `ProblemDetails` con `correlationId`; los 500 no exponen detalles (ADR-003).
- Orden por lista blanca (`SortMap`) con desempate por `Id` (ADR-004).
- Swagger solo en Development.
- El bloque 1.4 del plan original se separó en 1.4 y 1.5 para mantener un commit por tema.
- Se desactiva CA1848 (`LoggerMessage`) por simplicidad.

## Pendientes y notas para los siguientes sprints

- Aplicar la migración a la base de desarrollo cuando haya entidades reales.
- Pruebas del sellado de `CreatedAtUtc` y `UpdatedAtUtc`: se escriben en el bloque 2.1, con la primera entidad.
- `EnableRetryOnFailure` no está activo; si se activa, la transacción de factura debe usar `CreateExecutionStrategy()` (Sprint 5).
- El requisito de seguridad Bearer por endpoint se aplica en el bloque 2.3.
- Descuento de facturas (por línea, global o ambos): se decide en el bloque 5.1.
- Los controladores de prueba (`ErrorsTestController`, `PagingTestController`) viven solo en el proyecto de pruebas.
