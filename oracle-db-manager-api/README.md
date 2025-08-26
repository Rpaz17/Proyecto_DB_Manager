# Oracle DB Manager API (.NET 8, C#)

## Prereqs

- .NET 8 SDK
- Oracle Managed Data Access .NET Core provider (nuget restores automatically)
- An Oracle database you can reach

## Setup

```bash
dotnet restore
dotnet build
dotnet run
```

The API will launch (default http://localhost:5000). Swagger UI is enabled at `/swagger`.

## Configure JWT

Edit `appsettings.json`:

- `Jwt:Key` → set a long random string
- `Jwt:AccessTokenMinutes` → default 15
- `Jwt:RefreshTokenHours` → default 8

## Auth Flow

1. `POST /api/auth/connect` with JSON:

```json
{
  "conn": {
    "host": "localhost",
    "port": 1521,
    "service": "FREEPDB1",
    "user": "sys",
    "password": "OR@c13adm",
    "privilege": null
  }
}
```

On success returns `{ accessToken, refreshToken, expiresAt }`. 2. Use `Authorization: Bearer <accessToken>` for subsequent calls. 3. When near expiry, `POST /api/auth/refresh` with `{ "refreshToken": "<refresh>" }`.

## Endpoints

- `GET /api/metadata/schemas`
- `GET /api/metadata/tree` → hierarchical list of schemas → tables/views/procedures/functions
- `POST /api/sql/execute` → `{ "sql": "select * from ...", "maxRows": 500 }`
- `POST /api/ddl/create-table` → with columns, PK
- `POST /api/ddl/create-view`
- `POST /api/ddl/create-routine` → source text for procedure/function

> Note: The service opens a new OracleConnection per request using your session's connection string.
