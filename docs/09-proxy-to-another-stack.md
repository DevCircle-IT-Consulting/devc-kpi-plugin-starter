# 09 - Install an on-prem proxy (reporting on a database the engine can't reach)

Your source database often lives somewhere the KPI engine can't (or shouldn't) reach directly — inside
**its own application stack** (an ERP/CRM/line-of-business app with its own Postgres/SQL Server
container), or on a network only an on-site host can see. The **on-prem proxy** bridges that: a small
agent you run *next to the database* which dials **out** to your engine and runs the engine's queries
against the local DB. Nothing inbound is exposed on the DB side.

```
KPI engine  ⇜ relay (the proxy dials OUT) ⇜  proxy  ──▶  the database (reachable from the proxy)
```

## The one mental model to get right

The proxy holds **no** database config. The engine sends the **connection string** (from its own secret
store) down to the proxy with each query, and the proxy runs it. So the connection string must name the
DB **as the proxy sees it** — e.g. the DB container's service name on the network the proxy joins
(`Server=app-db`), *not* a host the engine could reach.

## The credentials, demystified

This is the part that trips people up. A proxy install involves exactly **two** secrets:

| Secret | What it is | Where the plaintext lives | Where it goes on the server |
|---|---|---|---|
| **Proxy API key** | authenticates *this proxy's* connection to the engine | on the proxy host only | only its **SHA-256 hash**, under `Reporting:Secrets:PROXY_<ID>` |
| **DB connection string** | the credentials the engine sends down for each query | on the server | `Reporting:Secrets:<NAME>` (e.g. `APP_DB`) |

> **Not the same as `keygen`.** The `ProxyProbe keygen` / relay-key flow (docs/07) is a *developer*
> credential for interactively exploring a customer DB. It is **not** used for a normal proxy install and
> you don't need it here. A proxy install only ever deals with the two secrets above.

The proxy key is generated **where the proxy runs** and never leaves that host — the server only stores
its hash. You wire the two together by name: the `proxies.yaml` entry's `secret:` field names the server
secret that holds the hash.

---

## Step 1 — Register the proxy on the server (`proxies.yaml`)

Edit **`server/config/proxies.yaml`** (prod: `/srv/kpi/config/proxies.yaml` — server-owned, per install).
Add an entry:

```yaml
proxies:
  - id: acme-onprem1                 # unique id; a datasource references this in `proxy:`
    name: "ACME on-prem proxy"       # shown on the tenant-admin Proxies page
    secret: PROXY_ACME_ONPREM1       # the server secret that will hold the key HASH (Step 3)
    tenants: [acme]                  # which tenant(s) may use it — a list, or the scalar `all`
```

A proxy entry is pure identity + scope: **no url, no key, no connection string** — those live elsewhere.
Convention for the id: `<tenant>-live1` / `<tenant>-test1`.

While you're here you can also drop in the **connection-string** secret (Step 3 covers it) so you only
restart once.

## Step 2 — Reload the engine so the proxy appears

The engine reads `proxies.yaml` at startup, so after editing it:

```bash
cd server            # or /srv/kpi on a prod box
docker compose restart api
```

*(This is the easily-forgotten step — the config edit alone does nothing until the API reloads.)* The
proxy now shows up on the tenant-admin **Proxies** page at **`/tenantadmin/proxies`**, with status
**Disconnected** (nothing is running on the DB side yet).

## Step 3 — Install & run the proxy where it can reach the DB

Pick the option that fits where your database lives. Both end with the proxy holding a key and the
engine holding that key's **hash**.

### Option A — as a native service (simplest; DB reachable from a Windows/Linux host)

On the tenant-admin **Proxies** page, click the **Install** button on the proxy's row. It shows a
ready-to-run one-liner (the page fills in your server URL and the proxy id). Run it **on the host that
can reach the database**:

```powershell
# Windows (run in an elevated PowerShell)
iwr "https://YOUR-KPI/downloads/DevC.KPI.Proxy-win-x64.exe" -OutFile DevC.KPI.Proxy.exe; `
  ./DevC.KPI.Proxy.exe install --url https://YOUR-KPI/api --proxy-id acme-onprem1
```

```bash
# Linux
curl -fsSL "https://YOUR-KPI/downloads/DevC.KPI.Proxy-linux-x64" -o DevC.KPI.Proxy \
  && chmod +x DevC.KPI.Proxy \
  && sudo ./DevC.KPI.Proxy install --url https://YOUR-KPI/api --proxy-id acme-onprem1
```

The installer **generates the proxy key on this host**, writes it to a local `proxy.settings.json`, and
**prints the key's HASH** in a box:

```
╭──── Add this key HASH to the KPI server's secret store ────╮
│  9f2c…（64 hex chars）…a7                                    │
╰────────────────────────────────────────────────────────────╯
```

**Copy that hash** — it goes into the server in Step 4. The plaintext key stays on this host; it is
never sent to or stored on the server. The installer registers an OS service (`DevC.KPI.Proxy` on
Windows, `devc-kpi-proxy` on systemd) that reconnects on boot.

### Option B — as a Docker container (DB lives in another Docker stack)

Use the [`../proxy`](../proxy) folder. Here you create the key/hash pair **by hand** (the container takes
the plaintext key via env; see [Appendix](#appendix-generate-a-proxy-key--hash-by-hand)):

```bash
KEY=$(openssl rand -hex 32)                        # the proxy key (plaintext)
printf '%s' "$KEY" | sha256sum | cut -d' ' -f1     # the HASH -> paste into the server in Step 4
echo "$KEY"                                         # the KEY  -> goes in the proxy .env below
```

Then:

```bash
cd proxy
cp .env.example .env         # set KPI_ENGINE_URL, PROXY_ID=acme-onprem1, PROXY_KEY=$KEY,
                             #     APPSTACK_NETWORK=<the DB stack's docker network>
docker compose up -d
docker compose logs -f proxy # should show it connecting to the engine's relay
```

`APPSTACK_NETWORK` is the external Docker network of the DB's stack (`docker network ls`, usually
`<project>_default`) so the proxy can resolve the DB by its container name.

## Step 4 — Put the key hash + the DB connection string on the server

Add a secret file under **`server/secrets/`** (prod: `/srv/kpi/secrets/`) — any `*.json` with the shape:

```jsonc
{
  "Reporting": {
    "Secrets": {
      "PROXY_ACME_ONPREM1": "<the key HASH from Step 3>",
      "APP_DB": "Server=app-db;Port=5432;Database=app;Username=reporting;Password=…;"
    }
  }
}
```

- `PROXY_ACME_ONPREM1` **must match** the `secret:` you put in `proxies.yaml` (Step 1) and holds the
  **hash**, never the key.
- `APP_DB` is the DB **connection string as the proxy sees the database** (Step "mental model"). Secret
  names are **global** — unique across all tenants.

Then reload so the engine picks up the new secret:

```bash
docker compose restart api
```

## Step 5 — Verify it's connected

Back on **`/tenantadmin/proxies`**, the proxy row should now show **Connected** with a recent *Last seen*.
If it shows an error, see [Troubleshooting](#troubleshooting).

## Step 6 — Point a datasource at the proxy

In your plugin's tenant config (`config/<tenant>/datasources/`), route the datasource through the proxy
by naming its id — everything else is the normal datasource shape (see docs/03):

```yaml
id: app                  # datasource id (widgets' DataSourceId + report pages reference it)
builder: AppCube         # the cube's Key
type: postgres           # postgres | mssql
proxy: acme-onprem1      # <- the id from proxies.yaml; OMIT for a directly-reachable DB
secret: APP_DB           # the connection-string secret from Step 4
```

Deploy the config, and your reports now build against the remote database through the proxy.

---

## Appendix: generate a proxy key + hash by hand

Only needed for **Option B** (Docker) — Option A's installer does this for you. The hash the server
expects is the **lowercase-hex SHA-256 of the UTF-8 key, with no trailing newline** (this is why the
commands use `printf '%s'`, not `echo`):

```bash
KEY=$(openssl rand -hex 32)
HASH=$(printf '%s' "$KEY" | sha256sum | cut -d' ' -f1)
echo "KEY  (-> proxy .env PROXY_KEY):        $KEY"
echo "HASH (-> server Reporting:Secrets):    $HASH"
```

PowerShell equivalent:

```powershell
$KEY  = -join ((1..32) | % { '{0:x2}' -f (Get-Random -Max 256) })
$HASH = ([System.BitConverter]::ToString(
          [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($KEY))
        ) -replace '-','').ToLower()
"KEY  = $KEY"; "HASH = $HASH"
```

## TLS trust

The proxy is a TLS client of your engine. With a **real certificate** this just works. With the
**self-signed** cert from the one-box setup, the proxy host (or container) must **trust that cert** or the
connection fails — install your CA/cert into the host's trust store, or mount it into the proxy container.
Prefer a real cert for anything the proxy talks to.

## Dev-access windows (optional, developer-only)

Normal cube **builds** run whenever the proxy is Connected. A **dev-access window** (the *Enable
dev-access 15/30/60* menu on the Proxies page) additionally, and temporarily, opens the interactive
*probe / ad-hoc query* path used while authoring a cube against a live customer DB (docs/07). It is not
needed for normal reporting and auto-expires. This is the only place the `ProxyProbe`/relay-key flow
comes in — and it's a developer convenience, not part of the install.

## Troubleshooting

- **Row stays Disconnected / `docker compose logs proxy` shows a TLS error** → the proxy doesn't trust
  your engine's certificate (see *TLS trust*).
- **Connects then aborts / auth error** → the hash on the server doesn't match the proxy's key. Re-check
  that `Reporting:Secrets:PROXY_<ID>` holds the *hash* the installer printed (Option A) or the hash of the
  exact `PROXY_KEY` in the proxy `.env` (Option B), that `PROXY_ID` matches the `proxies.yaml` `id`, and
  that you restarted the API after adding the secret.
- **Reports fail with a connection error but the proxy is Connected** → the connection string names the DB
  by a host the *proxy* can't resolve. It must be the DB's name on the proxy's network, not the engine's.
- **New secret seems ignored** → a newly-added secret file needs `docker compose restart api`.
