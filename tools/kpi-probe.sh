#!/usr/bin/env bash
#
# kpi-probe.sh - a convenience wrapper around the downloaded DevC.KPI ProxyProbe CLI, so you don't
# retype --url/--tenant/--token on every call. The probe explores a database THROUGH your deployed
# engine using safe verbs (schema/profile/validate) - it never returns a row value.
#
# One-time setup (put these in your shell profile):
#   export KPI_PROBE_BIN="$HOME/bin/devc-kpi-probe"        # path to the downloaded binary (see docs/07)
#   export KPI_PROBE_URL="https://kpi.yourco.example/api"  # your engine's API base (prod ends in /api)
#   export KPI_PROBE_TENANT="yourco"                       # your tenant name
#   export KPI_PROBE_TOKEN="<bearer>"                      # a TenantAdmin token  (OR set KPI_RELAY_KEY)
#
# Then:
#   ./tools/kpi-probe.sh schema   --ds bmd
#   ./tools/kpi-probe.sh schema   --ds bmd --table invoices
#   ./tools/kpi-probe.sh profile  --ds bmd --table invoices
#   ./tools/kpi-probe.sh validate --ds bmd --sql 'select "Id" from "Invoices" where "Tenant"=1'
#
# A dev-access window must be OPEN on the target engine's proxy (an operator enables it in the
# Proxies view); otherwise the safe verbs return 403.
set -euo pipefail

BIN="${KPI_PROBE_BIN:-devc-kpi-probe}"
URL="${KPI_PROBE_URL:?set KPI_PROBE_URL to your engine API base, e.g. https://kpi.yourco.example/api}"
TENANT="${KPI_PROBE_TENANT:-}"

if [ $# -lt 1 ]; then
  echo "usage: $0 <schema|profile|validate> [--tenant NAME] [--ds ID] [--table T] [--sql '<select>']" >&2
  exit 2
fi
verb="$1"; shift

# Let a --tenant argument override the env default without being passed twice (the CLI takes the
# first --tenant it sees). Everything else is forwarded verbatim.
rest=()
while [ $# -gt 0 ]; do
  case "$1" in
    --tenant) TENANT="$2"; shift 2 ;;
    *)        rest+=("$1"); shift ;;
  esac
done
[ -n "$TENANT" ] || { echo "set KPI_PROBE_TENANT or pass --tenant NAME" >&2; exit 2; }

# Auth: a bearer token if you have one, otherwise the CLI auto-resolves a relay key from
# KPI_RELAY_KEY / --relay-key-file (see the probe --help).
auth=()
[ -n "${KPI_PROBE_TOKEN:-}" ] && auth=(--token "$KPI_PROBE_TOKEN")

exec "$BIN" "$verb" --url "$URL" --tenant "$TENANT" \
  ${auth[@]+"${auth[@]}"} ${rest[@]+"${rest[@]}"}
