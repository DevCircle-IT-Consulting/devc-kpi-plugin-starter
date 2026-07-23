# 08 - Set up the server

To run reports you need a DevC.KPI engine. This stands up the full stack — PostgreSQL, the API, the
Blazor client, and a TLS reverse proxy — with Docker Compose, from the [`server/`](../server) folder.
The engine images are licence-gated: DevCircle gives you the registry host, a pull credential, and the
image tag.

## 1. Prerequisites
- Docker + Docker Compose.
- A **TLS certificate** for the host name you'll use — TLS is required (OpenIddict refuses non-HTTPS
  logins). Use Let's Encrypt or your CA for a public host; a self-signed cert is fine for internal use.
- The registry login + image tag from DevCircle.

## 2. Configure
```bash
cd server
docker login kpi.devcircle.at:5000                 # credential from DevCircle
cp .env.example .env                               # set POSTGRES_PASSWORD, KPI_IMAGE_TAG, KPI_UID/KPI_GID
cp api/appsettings.Production.template.json    api/appsettings.Production.json
cp client/appsettings.Production.template.json client/appsettings.Production.json
```
- In `api/appsettings.Production.json` set `Identity:EncryptionKey` (`openssl rand -base64 32`) — keep it
  constant, changing it invalidates all logins. If you serve on a real host name, set the `Identity` URLs
  there and the `ApiUrl`/`IdentityUrl` in the client file to that host (default: `https://localhost:8443`).
- Linux: set `KPI_UID`/`KPI_GID` to `id -u` / `id -g` so the non-root containers can write the
  bind-mounted `api/home`, `client/home`, and your config. (Docker Desktop: leave `1000`.)
- **Admin account**: by default the first run shows a one-time setup wizard where you pick the initial
  admin email + password (see step 4). To skip it for an automated install, set the initial admin via
  environment (in the `api` service of `docker-compose.yml`, or the `.env`):
  ```
  Seed__AdminEmail=admin@example.com
  Seed__AdminPassword=<a strong password>
  Seed__IncludeDemoLogin=false     # true also creates the public read-only demo login
  ```
  These are honoured only while the app is uninitialised (no admin yet); once an admin exists they are
  ignored.

## 3. Certificate
Place your cert + key at `certs/localhost.crt` and `certs/localhost.key` (the names `nginx.conf` expects;
change `server_name` there for a real host). A self-signed cert for internal use:
```bash
openssl req -x509 -newkey rsa:2048 -nodes -days 825 \
  -keyout certs/localhost.key -out certs/localhost.crt \
  -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost"
```

## 4. Start
```bash
docker compose up -d
docker compose logs -f api        # migrates + seeds the database on first start
```
Open `https://localhost:8443`. On a fresh database the app is **uninitialised** and shows a one-time
**setup wizard** — enter the initial admin email + password (and optionally tick "create the public demo
login"), submit, then log in with those credentials. The wizard is reachable only until an admin exists;
afterwards it refuses. The built-in Demo/Weather reports confirm the stack is up.

*(If you set `Seed__AdminEmail`/`Seed__AdminPassword` in step 2, the admin is created automatically and
the wizard is skipped — just log in.)*

## 5. Create your tenant
A plugin binds to a tenant by exact name (`config/<tenant>/` + `ForTenants("<tenant>")`). Create that
tenant in the admin area before deploying a plugin. *(Not sure where in the UI? Ask DevCircle — it's part
of onboarding.)*

## 6. Deploy a plugin
Build a bundle (`deploy/build-bundle.sh <Plugin> <tenant>`, see [05](05-build-and-deploy.md)), drop it in,
restart:
```bash
cp -r dist/bundles/<Plugin>/plugins/<Plugin>  server/plugins/
cp -r dist/bundles/<Plugin>/config/<tenant>   server/config/
docker compose restart api
docker compose logs api | grep "Reporting plugin"   # confirms it loaded
```
Report/YAML changes hot-reload; a new plugin DLL needs the `restart api`.

## Backups
The database is the Docker volume `kpi-pgdata`; back it up with `pg_dump` (or DevCircle's backup script).
`api/home` holds the signing keys — keep it too, or logins reset on restore.
