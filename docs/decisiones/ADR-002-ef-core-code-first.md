# ADR-002: EF Core Code First con SQL Server

- **Estado:** Aceptada
- **Fecha:** 2026-11-19

## Contexto

Se necesita persistencia relacional con migraciones versionadas, auditoría uniforme y control de concurrencia optimista en todas las entidades.

## Decisión

- **Code First** con migraciones en `Infrastructure/Persistence/Migrations`. El esquema se deriva de las entidades de Domain.
- **`BaseEntity`** (Domain) con `Id` (int identity), `CreatedAtUtc`, `UpdatedAtUtc` y `RowVersion`.
- **Auditoría:** `AppDbContext` sella `CreatedAtUtc` y `UpdatedAtUtc` al guardar, usando un `TimeProvider` inyectado (controlable en pruebas).
- **Concurrencia:** `RowVersion` se configura como `rowversion` para todo tipo que herede de `BaseEntity`. Detalle de uso para stock en el Sprint 4.
- **Dinero:** convención global `decimal(18,2)`; ninguna propiedad decimal queda con precisión por defecto.
- **Abstracción:** los servicios usan `IAppDbContext` (Application); la implementación es `AppDbContext` (Infrastructure).
- **Reintentos:** no se activa `EnableRetryOnFailure` por ahora. Si se activa, las transacciones explícitas deberán envolverse en `CreateExecutionStrategy()` (se decide en el Sprint 5).
- Las migraciones generadas se excluyen de analizadores y de la exigencia de XML docs mediante `.editorconfig`.

## Consecuencias

- Toda entidad nueva obtiene auditoría y concurrencia sin código adicional.
- Las pruebas de integración corren contra SQL Server real en contenedor (Testcontainers), compartido entre clases de prueba.
