# 11 - Users & report rights

Who can do this: **Admin** or **TenantAdmin**. Users are managed at **`/person`** (the "Users" nav
entry); a TenantAdmin manages a specific tenant's users from **Tenants → a tenant → Users**
([14](14-tenants-and-admin-tools.md)).

## Email (recommended, not required)

Creating a user produces a one-time **set-password link**. With a mail sender configured (SMTP `Email`
or `MSGraphMail` — see [08 §Configure](08-run-the-server.md)), it's emailed to the user automatically as
an invitation. **Without** a mail sender, onboarding still works: the app shows the link on screen right
after you save, so you copy it and pass it to the user through your own channel. Password resets behave
the same way. Configuring email is smoother, but it is no longer a hard prerequisite.

## Create a user

1. Go to **`/person`** → **Create user** (Admin).
2. Fill in:
   - **Name** — display name.
   - **Email** — this is also the **login username**. (It becomes read-only once the login exists.)
   - **Role** — **Admin** or **User**. (TenantAdmin is not offered here — see [10](10-operating-overview.md).)
3. On the **Reports** tab, tick the reports this user may see (you can also do this later — see below).
4. **Save.** The user is created. If email is configured, an **invitation** with a set-password link is
   emailed to them. If not, a dialog shows that link — **copy it and send it to the user** yourself.
   Either way, they open the link, choose a password, and can sign in.

The user list shows a **password** indicator per row — filled once the person has completed the
invitation (set a password). Empty means the invite is still outstanding.

## Grant report access (the important part)

**Report access is an explicit, per-user grant. Empty = the user sees no reports — and there is no admin
bypass** (an Admin with no grants also sees nothing). A brand-new user starts with **none**.

Two ways to grant, both editing the same per-user list:

- **Per user** — in the user editor, the **Reports** tab: tick the reports for that one person.
- **Bulk / across everyone** — **`/person/report-rights`** (the **Report rights** button on the Users
  page): a **users × reports matrix**. Tick a cell to grant one user one report; use the row/column
  "grant all" / "clear" controls to do many at once. Each change saves immediately.

If a user reports "I can't see any reports", this is almost always the answer: they have no grants yet.

## Reset a password / re-send an invitation

- **Admin-initiated:** open the user in the editor → **Reset password** — emails a fresh set-password
  link (or, if email isn't configured, shows it on screen for you to copy). Use this if the original link
  expired.
- **Self-service:** the login page's **Forgot password?** link — the user enters their email and gets a
  reset link. (Also requires mail to be configured.)

Reset links expire; if one has, just re-send (the reset page offers a fresh link on an expired one).

## Change a role or remove a login

- **Change role** (Admin ↔ User): edit the user, change **Role**, save. You cannot rename the
  username/email of an existing login (create a new user instead).
- **Remove the login** but keep the person record: clear the username in the editor and save — the login
  is deleted; the report grants/person remain.

## Who can manage whom

- **Admin** manages users **within their own tenant**.
- **TenantAdmin** manages users in **any** tenant (via Tenants → tenant → Users), and can **impersonate**
  a user to see exactly what they see ([14](14-tenants-and-admin-tools.md)).
