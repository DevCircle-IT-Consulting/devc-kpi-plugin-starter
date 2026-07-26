# 13 - Operations (backup, restore, update, logs)

Everyday running of the [`server/`](../server) stack. Run these from the `server/` directory (where your
`docker-compose.yml` and `.env` live). Commands read the DB name/user from inside the `postgres`
container, so you don't need to retype credentials.

## What to back up

Two things — the rest is rebuildable from your images and config:

1. **The database** — the Postgres volume `kpi-pgdata` (all users, tenants, report grants, share links).
2. **`server/api/home`** — the API's **signing/encryption keys** (OpenIddict + DataProtection). Lose it
   and every issued token/cookie is invalidated (everyone must log in again) — so back it up alongside
   the DB.

*(Your TLS `certs/`, `config/`, `secrets/` and `.env` are worth keeping too, but they're things you
authored — the two above are the live state.)*

## Back up the database

```bash
cd server
docker compose exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"' > kpi-$(date +%Y%m%d-%H%M%S).dump
```

Produces a `pg_dump` custom-format archive. Also snapshot the keys:

```bash
tar czf api-home-$(date +%Y%m%d).tgz api/home
```

Automate by putting the first command in a cron job (keep, say, the newest 14).

## Restore the database

Restore **before** the API starts, so the app migrates the restored schema forward instead of racing an
empty DB:

```bash
cd server
docker compose up -d postgres                     # DB only
# wait until it's ready:
docker compose exec -T postgres sh -c 'until pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"; do sleep 1; done'
docker compose exec -T postgres sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner --no-privileges' < kpi-YYYYMMDD-HHMMSS.dump
docker compose up -d                              # bring up the rest
```

Restore the keys too if you're moving to a new host: `tar xzf api-home-YYYYMMDD.tgz` into `server/`.

## Update the engine

DevCircle publishes new engine images to the registry. To update:

```bash
cd server
# edit .env -> set KPI_IMAGE_TAG to the tag DevCircle gives you (or a moving tag like `test`/`latest`)
docker compose pull            # fetch api + client + (if used) proxy at that tag
docker compose up -d           # recreate the changed containers
docker compose logs -f api     # watch it apply DB migrations + come up
```

- **DB migrations apply automatically** on API start — no manual step.
- There's a **brief blip** while `api`/`client` recreate; the reverse proxy serves a "please wait" page
  until they're back.
- **Moving tags** (`test` / `latest`) don't auto-pull — you must `docker compose pull` to get the newest
  image behind the tag, then `up -d`. A fixed version tag (e.g. `1.4.1.42`) is immutable.
- If a migration crosses a breaking change, take a DB backup first (above).

## Restart after a config change

- **Report / datasource YAML** hot-reloads — no restart.
- **A new plugin DLL**, a **new secret file**, or **`proxies.yaml`** changes need: `docker compose restart api`.
- **`appsettings.Production.json`** changes need a restart too: `docker compose restart api` (or `client`).

## Logs & health

```bash
docker compose ps                 # container states
docker compose logs -f api        # follow the API log (migrations, plugin loads, errors)
docker compose logs api | grep -i "Reporting plugin"   # confirm your plugin loaded
curl -fsk https://localhost:8443/api/api/health        # 200 once DB is reachable + migrations ran
```

*(The `/api/api/health` doubling is intentional — the app is mounted under `/api` behind the proxy, and
its own health route is `/api/health`.)* `…/api/api/status` returns the running build version.

## Licence

Your engine needs a licence from DevCircle. `Licence:File` is the only licence setting, and the template
already has it right: `/srv/kpi/licence/licence.json`, inside the **writable** `./licence` bind mount from
`docker-compose.yml`. That mount matters — the engine writes the licence there itself when you activate and
renews it in place, so without it activation fails and anything already written would be lost the next time
the container is recreated. Back `./licence` up alongside `./api/home`.

**Activating.** A fresh installation keeps working for a grace period with no licence, so you can activate
it at your leisure: sign in as your cross-tenant admin, open **Licence**, and either

- paste the **activation key** you were sent — the engine fetches and installs its licence itself, or
- paste the **licence file** directly, if this host has no outbound internet access.

After that the engine keeps the licence current on its own; you should never have to touch it again.

**If it lapses.** The licence serves on for a grace period with a warning banner first. Only after that do
reports and share links stop — and the admin UI stays reachable throughout, so you can always install a new
licence. Nothing is deleted, so service resumes the moment a valid licence is in place.

```bash
curl -fsk https://localhost:8443/api/api/health/licence   # licence state; 200 healthy/degraded, 503 blocked
docker compose logs api | grep -i licence                 # what the engine decided, and why
```

The licence endpoint is deliberately **separate** from `/api/api/health`, so an expired licence never puts
the site into maintenance mode.

> The number of **datasources** your licence covers is counted as distinct secret-backed database
> connections across the whole installation — several cubes reading one database count once. Over the cap the
> engine keeps running and simply does not activate the surplus datasources; the Licence page names them.

## TLS & changing the host name

Certificates live in `server/certs/` (referenced by `nginx.conf`; `server_name` there is the host). If
you move to a real domain, update the cert **and** the URLs — `Identity:IdentityBaseUrl` /
`Identity:ClientAppBaseUrl` and `AllowedHosts` in `api/appsettings.Production.json`, and
`ApiUrl` / `IdentityUrl` in `client/appsettings.Production.json` — then `docker compose restart api client`.
See [08 §Configure](08-run-the-server.md).
