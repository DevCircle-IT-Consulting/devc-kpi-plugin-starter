# 08 - Run the server (one box, for evaluation)

To *build* a plugin you don't need a server (`dotnet test` is enough). To *see it live* you need a
running DevC.KPI engine to deploy it into. This page stands one up on a single machine.

> **Two honest caveats up front.**
> 1. The engine images are **licence-gated**. DevCircle gives you the registry host, a **pull
>    credential**, and the image **tag** to run. You cannot pull them anonymously.
> 2. This is an **evaluation** stack: it terminates TLS with a **self-signed** cert and runs the
>    containers as root for simplicity. A real deployment uses a proper certificate / your own
>    reverse proxy and runs non-root — arrange that with DevCircle. *(This quickstart hasn't been run
>    end-to-end from this repo yet — expect to tweak; the likely rough edges are TLS trust and the
>    exact admin path to create a tenant.)*

Everything below happens in the [`server/`](../server) folder.

## Prerequisites

- Docker + Docker Compose.
- From DevCircle: the registry host, a pull login, and the image tag.
- `openssl` (to make the local cert) and, on the pull host, `docker login`.

## Steps

1. **Log in to the registry** (credential from DevCircle):
   ```bash
   docker login kpi.devcircle.at:5000
   ```
2. **Environment** — copy and fill:
   ```bash
   cd server
   cp .env.example .env         # set POSTGRES_PASSWORD and KPI_IMAGE_TAG
   ```
3. **App config** — copy the templates and set a stable encryption key:
   ```bash
   cp api/appsettings.Production.template.json    api/appsettings.Production.json
   cp client/appsettings.Production.template.json client/appsettings.Production.json
   # in api/appsettings.Production.json set Identity:EncryptionKey to: openssl rand -base64 32
   ```
   The URLs default to `https://localhost:8443` — fine for local. For a real hostname, set it in both
   files.
4. **TLS cert** (self-signed, for localhost):
   ```bash
   openssl req -x509 -newkey rsa:2048 -nodes -days 825 \
     -keyout certs/localhost.key -out certs/localhost.crt \
     -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost"
   ```
5. **Start it:**
   ```bash
   docker compose up -d
   docker compose logs -f api      # watch it migrate + seed the database
   ```
6. **Open** `https://localhost:8443` and accept the self-signed-certificate warning. Log in with the
   seeded admin (`admin@devcircle.at` / `DevCircleIsThe1Admin!`) and **change that password immediately**.

You should see the built-in **Demo / Weather** reports — they're baked into the image, so a fresh
server is never empty. That confirms the stack works.

## Create your tenant

Your plugin binds to a **tenant** by exact name (`config/<tenant>/` + `ForTenants("<tenant>")`). Before
deploying a plugin, create that tenant in the running engine's **admin area** (name it e.g. `scouts`) so
the name exists. *(If you can't find where to create tenants in the UI, ask DevCircle — tenant
provisioning is part of onboarding, and the exact path may differ by version.)*

## Deploy your plugin into this server

1. Build a bundle from your plugin repo: `deploy/build-bundle.sh <Plugin> <tenant>` (see
   [05-build-and-deploy.md](05-build-and-deploy.md)).
2. Copy it into this stack's volumes and restart the API:
   ```bash
   cp -r dist/bundles/<Plugin>/plugins/<Plugin>  <path-to>/server/plugins/
   cp -r dist/bundles/<Plugin>/config/<tenant>   <path-to>/server/config/
   docker compose restart api
   ```
3. Confirm it loaded:
   ```bash
   docker compose logs api | grep "Reporting plugin"
   ```
   Grant the new report to your users, and it appears. Report/YAML changes hot-reload; a new **plugin
   DLL** needs the `restart api`.

## Going beyond evaluation

For a real deployment (proper TLS, a public hostname, non-root containers, backups, the private-registry
pull model), that's a DevCircle-assisted install — this folder is the local starting point, not the
production runbook.
