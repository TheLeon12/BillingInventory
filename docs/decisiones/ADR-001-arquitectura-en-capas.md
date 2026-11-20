# ADR-001: Arquitectura en capas

- **Estado:** Aceptada
- **Fecha:** 2026-11-19

## Contexto

El proyecto debe demostrar buenas prácticas de backend y separar la lógica de negocio de la infraestructura para poder probarla de forma aislada.

## Decisión

Cuatro proyectos en `src/`:

| Capa | Responsabilidad | Depende de |
|---|---|---|
| Domain | Entidades y reglas de negocio puras | nada |
| Application | Servicios, DTOs, validaciones (FluentValidation), interfaces | Domain |
| Infrastructure | EF Core, SQL Server, PDF (QuestPDF), hash de contraseñas | Application |
| Api | Controllers, middleware, DI, Swagger | Application, Infrastructure |

- Las entidades nunca se exponen en la API; se usan DTOs.
- Los servicios usan `DbContext` mediante una interfaz definida en Application. No se crean repositorios genéricos por entidad, porque EF Core ya implementa Unit of Work y Repository; una capa más solo añadiría código sin valor.
- Ajustes de compilación comunes en `src/Directory.Build.props`: nullable, documentación XML obligatoria y warnings tratados como errores.

## Consecuencias

- La lógica de negocio se prueba con xUnit sin base de datos.
- Las pruebas de integración usan SQL Server real (contenedor), necesario para validar transacciones y concurrencia.
- Application queda ligada a EF Core a través de la interfaz del contexto; es un compromiso aceptado a cambio de simplicidad.
