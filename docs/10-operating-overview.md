# 10 - Operating DevC.KPI (admin overview)

Docs 00–07 are for **building reports**. This one starts the **operator track** — for whoever *runs and
administers* a DevC.KPI server: creating users, granting report access, sharing reports, backups and
updates.

- **[08 - Run the server](08-run-the-server.md)** — stand the stack up (first, once).
- **10 - this page** — roles, the admin UI, and the first-day checklist.
- **[11 - Users & report rights](11-users-and-report-rights.md)** — create users; **grant report access**.
- **[12 - Sharing reports by link](12-sharing-reports.md)** — login-free public links.
- **[13 - Operations](13-operations.md)** — backup, restore, update, logs.
- **[14 - Tenants & admin tools](14-tenants-and-admin-tools.md)** — tenants, query console, diagnostics.
- **[09 - On-prem proxy](09-proxy-to-another-stack.md)** — reach a database the engine can't (when needed).

## The one thing to know first

> **A new user sees _no reports_ until you explicitly grant them.** Report access is a per-user grant
> with **no admin bypass** — even an Admin sees nothing until granted. If someone logs in to an empty
> report list, that's expected: go grant rights ([doc 11](11-users-and-report-rights.md)).

And for onboarding anyone: **email must be configured** (SMTP or Microsoft Graph — see
[08 §Configure](08-run-the-server.md)). User invitations and password resets are delivered *only* by
email; there is no in-app "copy invite link". Configure mail before you create users.

## Roles

Three roles, set on the user (except TenantAdmin — see below):

| Role | Who | Can do |
|------|-----|--------|
| **User** | a report consumer | View **only** the reports granted to them. Nothing administrative. |
| **Admin** | a tenant's administrator | Everything a User can, **plus** manage that tenant's users, grant report rights, and create share links. Still sees only the reports granted to *them*. |
| **TenantAdmin** | the server operator (you) | Cross-tenant super-admin. Manages **tenants**, **proxies**, the **query console**, and **diagnostics**; can create/edit users in any tenant and impersonate them. Not bound to one tenant. |

Notes that trip people up:
- **TenantAdmin is not assignable in the user editor** — the user-editor role dropdown offers only
  *Admin* and *User*. The first TenantAdmin is created by the **setup wizard** (or the `Seed:*` config)
  on first run ([08](08-run-the-server.md)). That is deliberate: TenantAdmin is the operator identity,
  not a per-tenant role.
- **TenantAdmin has no report list of its own.** It's an operator account (no tenant, no per-user report
  grants), so it sees the admin menus, not the Reports nav. To *view* reports as a tenant would, a
  TenantAdmin uses **impersonation** ([14](14-tenants-and-admin-tools.md)) or you give yourself an
  ordinary Admin/User account in that tenant.

## What each role sees in the UI

- **User / Admin** → the **Reports** section (their granted reports). Admin additionally sees **Share
  Links**.
- **TenantAdmin** → the admin cluster: **Tenants** (`/tenantadmin/tenant`), **Proxies**
  (`/tenantadmin/proxies`), **Query Console** (`/tenantadmin/query-console`), **Memory**
  (`/tenantadmin/memory`).
- Everyone signs in through the same login page; the setup wizard at `/setup` only appears on a
  brand-new, uninitialised install.

## First-day checklist

1. **Stand up the stack** and complete the **setup wizard** — pick your admin email + password. This
   account is a **TenantAdmin** ([08](08-run-the-server.md)).
2. **Configure email** (SMTP or MS Graph) in `api/appsettings.Production.json`, or invitations won't
   send ([08 §Configure](08-run-the-server.md)).
3. **Confirm your tenant.** A `default` tenant always exists; DevCircle provisions your real tenant
   name (it must match your plugin's `config/<tenant>/`). Create additional tenants in **Tenants**
   ([14](14-tenants-and-admin-tools.md)).
4. **Deploy your reporting plugin** into the tenant ([05](05-build-and-deploy.md)).
5. **Create users** and **grant report rights** ([11](11-users-and-report-rights.md)) — without step 2
   this stops here.
6. **Set up backups** ([13](13-operations.md)).
