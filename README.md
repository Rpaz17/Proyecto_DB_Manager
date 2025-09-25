# Proyecto_DB_Manager

OracleDBManager es un gestor de bases de datos ligero que se conecta a Oracle, permite explorar esquemas y ofrece herramientas para:

- **Sincronización de esquemas**: Realizar una migración inicial de tablas y datos desde Oracle hacia PostgreSQL.  
  _(Actualmente solo se soporta la primera sincronización; los cambios posteriores en el esquema no se reflejan automáticamente)._
- **Diagramas de modelo relacional**: Generar diagramas de base de datos en formato [Mermaid.js](https://mermaid.js.org/), incluyendo tablas, columnas y relaciones con claves foráneas.
- **Explorador de metadatos**: Inspeccionar tablas, vistas, procedimientos y funciones en Oracle.

---

## Instalación

### Requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Oracle Managed Data Access](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/)
- [Npgsql](https://www.nuget.org/packages/Npgsql/)

### Configuración del Backend
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/tu-usuario/OracleDBManager.git
   cd OracleDBManager

    Restaurar dependencias: dotnet restore

Ejecutar el backend:
    
    dotnet run 

    La API se iniciará (por defecto en http://localhost:5242).

Configuración del Frontend

    Navega al directorio del frontend (donde está app.js).

    Abre el HTML directamente en tu navegador o usa un servidor web ligero.

        Se utiliza TailwindCSS para los estilos.

        El JavaScript se conecta al backend mediante fetch.

Uso
1. Conectar a Oracle

Proporciona tu cadena de conexión en la interfaz o mediante cabeceras HTTP:

ConnectionString: User Id=hr;Password=hr;Data Source=localhost:1521/FREEPDB1

2. Sincronizar Oracle → Postgres

Envía una petición para iniciar la sincronización:

POST http://localhost:5242/api/sync/start-current

Headers:
  ConnectionString: <tu_cadena_conexion_oracle>
  PgConnectionString: <tu_cadena_conexion_postgres>

El backend realizará:

    Lectura de todas las tablas en Oracle.

    Creación de esquemas y tablas equivalentes en PostgreSQL.

    Copia de los datos.

Nota: Esto solo funciona para la primera sincronización. Si cambias los esquemas después, deberás re-sincronizar manualmente.
3. Generar Diagrama Relacional

Llama al endpoint para generar un diagrama en Mermaid.js:

GET http://localhost:5242/api/metadata/diagram
Headers:
  ConnectionString: <tu_cadena_conexion_oracle>
  
Tecnologías
    Backend: ASP.NET Core 8, C#
    Drivers de BD: Oracle.ManagedDataAccess, Npgsql
    Frontend: JavaScript, HTML, TailwindCSS
    Diagramado: Mermaid.js

Limitaciones

    La sincronización es solo inicial (no hay actualizaciones incrementales).
