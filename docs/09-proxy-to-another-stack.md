# 09 - Reporting on a database in another Docker stack (the proxy)

Your source database often lives in **its own application stack** — an ERP, a CRM, a line-of-business
app, each with its own Postgres/SQL Server container — that the KPI engine can't (or shouldn't) reach
directly. The **on-prem proxy** bridges that: a small container you run *on that stack's network* which
dials **out** to your engine's relay and runs the engine's queries against the local DB. Nothing inbound
is exposed on the DB side, and you point DevC.KPI at any number of such stacks the same way.

```
KPI engine  ⇜ relay (proxy dials out) ⇜  proxy container  ──▶  app-stack DB (same docker network)
```

## The one mental model to get right

The proxy holds **no** database config. The engine sends the **connection string** (from its secret
store) down the relay with each query, and the proxy runs it. So the connection string must name the DB
**as the proxy sees it** — i.e. the DB's container/service name on the network the proxy joins
(`Host=app-db`), *not* a host the engine can reach.

## Engine side (once per source stack)

1. **Register the proxy** in the engine's `config/proxies.yaml` (server/config/proxies.yaml in the
   [server](../server) stack):
   ```yaml
   proxies:
     - id: acme-onprem1
       name: "ACME on-prem proxy"
       secret: PROXY_ACME_ONPREM1     # -> the key HASH, below
       tenants: [acme]
   ```
2. **Add the secrets** to the engine (server/secrets, `Reporting:Secrets`):
   ```json
   {
     "Reporting": { "Secrets": {
       "PROXY_ACME_ONPREM1": "<sha256-hex-of-the-proxy-key>",
       "APP_DB": "Server=app-db;Port=5432;Database=app;Username=reporting;Password=...;"
     } }
   }
   ```
   `APP_DB`'s `Server=app-db` is the DB container's name on the proxy's network (see the model above).
   `PROXY_ACME_ONPREM1` is the **hash** of the proxy key.
3. **Generate the key + hash**: create a random key, give the plaintext to the proxy (`PROXY_KEY`) and
   the SHA-256 hash to the engine (`PROXY_ACME_ONPREM1`). The ProxyProbe `keygen` helper produces a
   pair; if unsure, ask DevCircle.
4. **The datasource** (in your plugin's `config/<tenant>/datasources/`) routes through the proxy:
   ```yaml
   id: app
   builder: AppCube
   type: postgres          # or mssql
   proxy: acme-onprem1     # the id from proxies.yaml
   secret: APP_DB
   ```

## Proxy side (next to the DB)

Use [`../proxy`](../proxy): fill `.env` (engine URL, `PROXY_ID`, `PROXY_KEY`, and the DB stack's docker
network) and `docker compose up -d`. The proxy connects out to the engine; `docker compose logs proxy`
shows the relay connection. The proxy image is `${KPI_IMAGE}/proxy` — same registry/tag as your engine.

## Two things to verify on your box

- **TLS trust.** The proxy is a TLS client of your engine's relay. With a real certificate this just
  works; with the **self-signed** cert from the one-box setup, the proxy must trust that cert (mount your
  CA / the cert into the proxy container) — otherwise the relay connection fails. Prefer a real cert for
  anything the proxy talks to.
- **Dev-access window.** A window gates the interactive *probe / ad-hoc* path; normal cube **builds** run
  whenever the proxy is connected and authenticated. Confirm your reports build without a window open (if
  not, that's a gate to open for scheduled builds) — tell me what you see and I'll tighten this note.
