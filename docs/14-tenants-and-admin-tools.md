# 14 - Tenants & admin tools

TenantAdmin-only tooling. These are the entries in the admin nav cluster beyond Users and Proxies.

## Tenants — `/tenantadmin/tenant`

A **tenant** is an isolated space with its own users, config (`config/<tenant>/`), datasources and
reports. A `default` tenant always exists (it backs the built-in demo); DevCircle provisions your real
tenant name.

> **The tenant _name_ is a key, not a label.** It is matched by exact string against your plugin's
> `config/<tenant>/` and `PluginScope.ForTenants("<tenant>")` — see [02](02-config-reference.md). Set it
> to the exact name your plugin expects; it is not a display caption.

- **Create:** Tenants → **Create** → enter the **Name** → save.
- **Edit:** open a tenant. Beyond its name, an existing tenant has two tabs:
  - **Users** — that tenant's users (same create/invite/rights flow as [11](11-users-and-report-rights.md),
    scoped to this tenant), plus **Impersonate**: act *as* a chosen user and see exactly what they see
    (the app reloads in their identity; leave impersonation from the top bar). This is how a TenantAdmin
    — who has no report list of their own — verifies what a tenant's users actually get.
  - **Share Links** — that tenant's [share links](12-sharing-reports.md).

Deploying the reporting **plugin + config** for a tenant is the plugin repo's job
([05](05-build-and-deploy.md)); this page manages the tenant record and its people.

## Query console — `/tenantadmin/query-console`

A read-only SQL console for a **proxied** datasource (Postgres/MSSQL reached through an on-prem
[proxy](09-proxy-to-another-stack.md)). Use it to sanity-check data or explore a schema while authoring
a cube. It:

- lets you pick a proxy + datasource, cap the row count, and run a `SELECT` (Ctrl+Enter);
- has a schema browser (tables/columns) and click-to-insert `select * from …`;
- keeps a short query history and exports results as CSV / JSON / Markdown (and a "copy for an LLM" block).

It runs **read-only** queries through the same authenticated path your reports use (no dev-access window
needed). **It returns real row data**, so mind PII/DSGVO — a warning banner is always shown. For a
developer who must *not* see row values, use the ProxyProbe instead ([07](07-exploring-your-database.md)).

## Memory / diagnostics — `/tenantadmin/memory`

Read-only insight into the in-memory cube footprint — how much RAM your cached cubes use. KPI cards for
total cube memory, cube count, and the API process/host memory, plus a per-cube table (tenant,
datasource, estimated MB, node/leaf counts, build time, built-at) you can expand per query. A **Copy
report** button emits a heaviest-first text summary to paste into a support chat. Nothing is built just
to fill this page — it reflects cubes as they were last built. (An Admin sees only their own tenant's
cubes; a TenantAdmin sees all.)
