# HelloWorld — install profile

This is the one file to fill in for **your** deployment. Claude reads it (see [CLAUDE.md](CLAUDE.md)) so
it can reach your engine, download the probe, and scaffold datasources without re-asking every session.
It holds **no secrets** — only names, ids and URLs — so it is safe to commit.

Replace every `<…>` and pick one of the `|`-separated options. Delete this line when done.

---

## How to start (paste into Claude)

> Read PLUGIN.md and let's build this DevC.KPI plugin. Install the skill, then start by exploring the
> database (schema first, then profile the tables that matter). Ask me only for anything PLUGIN.md
> doesn't already answer.

## Engine & hosting

- **Hosting:** self-hosted (I deploy by hand) | DevCircle-hosted
- **Tenant name (exact):** helloworld
  <!-- Must EQUAL the tenant the engine knows — not a label. Confirm with your operator; it may be
       `default` on a single-tenant box. If you change it, also update PluginScope.ForTenants(...). -->
- **DNS topology:** single-DNS (one host, API under `/api`) | two-DNS (API on its own root name)
- **Web host** (open the app / download the probe here): `https://<web-host>`
- **API base** (`--url` for the probe; = the client's `ApiUrl`):
  `https://<web-host>/api` (single-DNS)  |  `https://<api-host>` (two-DNS, no `/api`)

## Access (for exploring the DB)

- **Auth:** relay key set (`KPI_RELAY_KEY` — preferred; mint once with the probe's `keygen`) | I'll paste a short-lived TenantAdmin bearer token | I'll run the probe myself
- **Dev-access window:** I'll open it (Proxies → Enable 15/30/60) | ask <who> to open it
  <!-- The safe probe verbs return 403 unless a dev-access window is open on the proxy. -->

## Data sources

One row per source this plugin reports on (leave the example, edit the values):

| datasource id | proxy id | secret name | DB type | what's in it (tables / meaning) |
|---------------|----------|-------------|---------|---------------------------------|
| `<id>`        | `<proxy or —>` | `<SECRET_NAME>` | postgres \| mssql \| custom | `<e.g. membership, attendance, finances, events>` |

- **Datasource YAML exists on the server yet?** no — scaffold a minimal `proxy:`+`secret:` binding first |
  yes: `<ids>`
- `proxy` is only for a DB the engine can't reach directly (see `docs/09`); leave `—` for a direct DB.

## Deploy (how the built plugin reaches the engine)

- **Self-hosted, by hand:** `bash deploy/build-bundle.sh HelloWorld helloworld`, then copy
  `dist/bundles/HelloWorld/plugins/HelloWorld/` → `/srv/kpi/plugins/HelloWorld/` and `config/helloworld/`
  → `/srv/kpi/config/helloworld/`, then `docker compose restart api`. **Who runs it:** me | <who>.
- **DevCircle-hosted:** hand the bundle to DevCircle. See `docs/05`.
