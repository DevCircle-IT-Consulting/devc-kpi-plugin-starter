# 12 - Sharing reports by link

A **share link** lets someone view one report **without logging in** — a random, unguessable URL you can
hand out and revoke. Read-only. Useful for stakeholders who shouldn't have an account.

Who can create them: **Admin** or **TenantAdmin** only. A plain **User cannot** — minting a public link
is an administrative act (it exposes a report outside the login).

## Create a link

Either from the report itself (its **Share** button) or from **`/sharelinks`** (the **Share Links** nav
entry) → **New link**. In the dialog:

- **Report** — which report (pre-filled if you came from a report's Share button).
- **Scope** — the **whole report**, or a **single page** of it.
- **Name** / **Notes** — for your own bookkeeping (who it's for, why).

Save, and you get the public URL to copy:

```
https://YOUR-KPI/share/<token>
```

Anyone with that URL sees the report (or the one page) read-only, with no login — it uses the same
public endpoints as the authenticated viewer, and the token grants exactly that scope.

## Manage & revoke

**`/sharelinks`** lists your active links (Name, Report, Scope, Notes, URL). Per link:

- **Copy** the URL.
- **Revoke** — the link stops working immediately. Revoked links are kept for the audit trail but no
  longer resolve.

## What to keep in mind

- **The link _is_ the credential.** Anyone who has it can view — treat it like a password. Share it over
  a trusted channel and **revoke** it when it's no longer needed.
- **No expiry.** Links don't time out; they're valid until revoked. For time-boxed access, revoke when
  done.
- **Read-only & scoped.** A viewer can't navigate to other reports or pages, change data, or reach
  anything else — only the report (or page) the link was minted for.
- Links are **per tenant**; a TenantAdmin mints them for a specific tenant from that tenant's
  **Share Links** tab ([14](14-tenants-and-admin-tools.md)).
