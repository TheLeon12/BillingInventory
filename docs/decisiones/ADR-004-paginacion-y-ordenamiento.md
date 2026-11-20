# ADR-004: Paginación, filtros y ordenamiento compartidos

- **Estado:** Aceptada
- **Fecha:** 2026-11-19

## Contexto

Todos los listados deben ofrecer paginación, filtros y ordenamiento con metadatos de página, de forma consistente y segura (sin permitir ordenar por columnas arbitrarias ni devolver páginas inconsistentes).

## Decisión

- **Entrada:** `PagedQuery` (`page`, `pageSize`, `sortBy`, `sortDirection`) se enlaza desde la query string. Cada listado hereda de ella para añadir sus filtros (`ProductQuery : PagedQuery`).
- **Validación:** `PagedQueryValidator<T>` (FluentValidation) exige `page` entre 1 y 1 000 000 y `pageSize` entre 1 y 100. Los validadores de cada listado heredan de ella. Un valor inválido produce 400 con `errors` por campo.
- **Orden seguro:** `SortMap<T>` es una lista blanca de campos ordenables por entidad. Un campo desconocido lanza `ValidationException` (400) que lista los valores permitidos. Nunca se construye `ORDER BY` con texto del cliente.
- **Orden estable:** `SortMap` siempre añade `ThenBy(Id)`. Sin desempate, filas con el mismo valor pueden repetirse u omitirse entre páginas (verificado con una prueba contra SQL Server).
- **Salida:** `PagedResult<T>` con `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious` y `HasNext`. Los elementos son DTOs, nunca entidades.
- **Ejecución:** `ToPagedResultAsync` hace un `COUNT` y, solo si hay filas, un `SKIP/TAKE`. Los filtros opcionales usan `WhereIf`.
- **Valores por defecto:** página 1, tamaño 20, orden definido por cada listado.
- **Enumeraciones** se serializan como texto en toda la API (`JsonStringEnumConverter`).

## Alternativas descartadas

- Paginación por cursor (keyset): más eficiente en tablas enormes, pero los listados de este proyecto son pequeños y los reportes necesitan saltar a páginas concretas y mostrar el total.
- `System.Linq.Dynamic` u ordenar por nombre de propiedad con reflexión: permite ordenar por cualquier columna, incluidas las que no deberían exponerse.

## Consecuencias

- Cada listado nuevo solo define su `SortMap`, sus filtros y su validador.
- `SortMap` ordena entidades (hereda de `BaseEntity`); la proyección a DTO se hace después de ordenar.
- El `COUNT` extra cuesta una consulta por petición; es aceptable a esta escala.
