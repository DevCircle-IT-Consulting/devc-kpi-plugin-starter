# proxy/ — run the on-prem proxy next to another stack's database

When the database you want to report on runs in **its own Docker stack** (an application's Postgres/SQL
Server that the engine can't reach directly), run the **DevC.KPI proxy** as a container on that stack's
network. It dials **out** to your KPI engine's relay and answers the engine's queries against the DB —
nothing inbound is exposed on the DB side.

```
your KPI engine  ⇜ relay (outbound) ⇜  proxy container  ──▶  app-stack DB (same docker network)
```

This is the clean way to point DevC.KPI at any containerized application stack (an ERP, a CRM, a
line-of-business app, …) without joining networks to the engine or exposing the DB.

## Use it

```bash
cp .env.example .env      # engine URL, proxy id + key, and the DB stack's docker network
docker compose up -d
docker compose logs -f proxy    # should connect to the engine's relay
```

Full wiring — the engine-side entries (proxies.yaml, the key hash, the datasource + connection string)
and how the connection string must name the DB *as the proxy sees it* — is in
[../docs/09-proxy-to-another-stack.md](../docs/09-proxy-to-another-stack.md).
