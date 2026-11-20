# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [No publicado]

### Añadido
- Bloque 1.1: estructura de la solución en capas, `Directory.Build.props` (nullable, XML docs y warnings como errores), `appsettings.Example.json`, README inicial y ADR-001.

- Bloque 1.2: `BaseEntity` (Id, auditoría, `RowVersion`), `IAppDbContext`, `AppDbContext` con precisión `decimal(18,2)`, registro de DI, migración `InitialCreate`, fixture de pruebas con SQL Server en Testcontainers y ADR-002.

- Bloque 1.3: Serilog (consola y archivo JSON), `CorrelationIdMiddleware`, `GlobalExceptionHandler` con `ProblemDetails`, excepciones tipadas (`NotFoundException`, `ConflictException`, `BusinessRuleException`), pruebas del pipeline de errores y ADR-003.

### Cambiado
- `appsettings*.json`: la sección `Logging` se reemplaza por `Serilog`.

### Eliminado
- Archivos de plantilla (`WeatherForecast`, `Class1`, `UnitTest1`).
