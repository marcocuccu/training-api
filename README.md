# Training API

ASP.NET Core Web API project focused on backend development practices.

## Tech stack

- C#
- ASP.NET Core Web API
- SQL Server
- Dapper
- JWT authentication
- Refresh tokens
- Docker
- xUnit


## Architecture

The project follows a layered structure:

```text
Controller → Service → Repository → DataContext → SQL Server
```

### Main responsibilities:

- Controllers handle HTTP requests and responses
- Services contain logic
- Repositories handle database access
- DTOs represent API contracts, different from database entities
- Centralized handle exceptions at Middleware level
- JWT authentication required for authorized endpoints


## Features
- User registration
- User login/logout
- JWT access token generation
- Refresh token 
- CRUD operations
- Docker configuration (both DB and API)
- Automatic database creation and seed scripts through Docker Compose


## Local setup with Docker

Create a local `.env` from `.env.example` and update values:
```bash
cp .env.example .env
```

Start the full stack:
```bash
docker compose up --build
```

This will start:

- SQL Server
- DB initialization container
- ASP.NET Core API

The API will be available at:

http://localhost:8080/swagger

SQL Server will be available from the host machine at: `localhost,1433`
Default database: `MainDB`

Stop the stack:
```bash
docker compose down -v
```
`-v` removes the database volume.


## Database initialization

The database is initialized automatically by the db-init service in `docker-compose.yml`
SQL scripts are located in: `db/scripts/`

Current scripts:

- 1_create_database.sql
- 2_create_schema.sql
- 3_seed_data.sql

The initialization creates:

- MainDB
- MainSchema.Users
- MainSchema.Auth
- MainSchema.RefreshTokens
- MainSchema.CleanExpiredTokens



## Configuration

Runtime configuration is provided through environment variables.

The public repository includes:`.env.example`

The local file:`.env` must not be committed.

Required variables:

- DB_NAME
- MSSQL_SA_PASSWORD
- PASSWORD_PEPPER
- JWT_KEY
- JWT_ISSUER
- JWT_AUDIENCE
- JWT_ACCESS_TOKEN_EXPIRES_MIN
- JWT_REFRESH_TOKEN_EXPIRES_MIN

### Configuration files

The application uses `Training/appsettings.json` as default configuration file.

That means, values in `appsettings.json` are used when the API is run directly, i.e. by using:

```bash
dotnet run --project Training
```

When the API is run through Docker Compose, values in `appsettings.json` are overridden by values in `docker-compose.yml`.


### Development database user

This Docker Compose setup uses the SQL Server `sa` user for local development simplicity.
This is acceptable for such project but, in a production scenario, the API should use a dedicated database user with limited permissions.
