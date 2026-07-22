#!/usr/bin/env bash
#
# build-bundle.sh — assemble a deployable "tenant bundle" for the DevC.KPI volume model
# (monorepo-split plan §4). A bundle is what drops into the deployment volume the engine scans:
#
#   <bundle>/
#   ├── plugins/<Plugin>/DevC.KPI.Plugins.<Plugin>.dll (+ .deps.json + any PRIVATE deps)
#   ├── config/<tenant>/         (datasources + reports + plugins.yaml)   [if the tenant has config]
#   └── secrets/<tenant>.example.json                                     [if present]
#
# The plugin folder deliberately ships ONLY the plugin assembly and its genuinely-private
# dependencies. The engine, the SDK, LinqCube, the DB drivers and the framework are provided by
# the base image and MUST NOT be duplicated here — the runtime load context resolves those to the
# host's copy (PluginLoadContext), so a duplicate would only add weight (and, without the identity
# guard, would break type identity). Shared assemblies are filtered out below.
#
# Usage:
#   deploy/build-bundle.sh <Plugin> [tenant] [--archive] [--out DIR]
#
#   <Plugin>    plugin project name, e.g. DataMeans -> src/DevC.KPI.Plugins.DataMeans
#   [tenant]    config/secrets tenant slug; default = lowercased <Plugin> (e.g. datameans).
#               A Shared plugin with no own config (e.g. TimeTracker) produces a plugin-only bundle.
#   --archive   also produce <out>/<Plugin>-bundle.tar.gz
#   --out DIR   output root (default: dist/bundles/<Plugin>)
#
# Examples:
#   deploy/build-bundle.sh DataMeans                 # plugin + config/datameans + secrets
#   deploy/build-bundle.sh TimeTracker               # plugin only (Shared: no own config)
#   deploy/build-bundle.sh Devc devc --archive       # explicit tenant + tarball
#
set -euo pipefail

# --- args -------------------------------------------------------------------------------------
PLUGIN=""
TENANT=""
ARCHIVE=0
OUT=""
while [ $# -gt 0 ]; do
  case "$1" in
    --archive) ARCHIVE=1 ;;
    --out) shift; OUT="${1:-}" ;;
    -h|--help) grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    -*) echo "ERROR: unknown option '$1'" >&2; exit 2 ;;
    *) if [ -z "$PLUGIN" ]; then PLUGIN="$1"; elif [ -z "$TENANT" ]; then TENANT="$1"; else
         echo "ERROR: unexpected argument '$1'" >&2; exit 2; fi ;;
  esac
  shift
done
[ -n "$PLUGIN" ] || { echo "ERROR: <Plugin> is required. See --help." >&2; exit 2; }
[ -n "$TENANT" ] || TENANT="$(printf '%s' "$PLUGIN" | tr '[:upper:]' '[:lower:]')"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJ="$REPO_ROOT/src/DevC.KPI.Plugins.$PLUGIN/DevC.KPI.Plugins.$PLUGIN.csproj"
CONFIG_DIR="$REPO_ROOT/config/$TENANT"
SECRET_EXAMPLE="$REPO_ROOT/secrets/$TENANT.example.json"
[ -n "$OUT" ] || OUT="$REPO_ROOT/dist/bundles/$PLUGIN"

[ -f "$PROJ" ] || { echo "ERROR: plugin project not found: $PROJ" >&2; exit 1; }

PLUGIN_DLL="DevC.KPI.Plugins.$PLUGIN.dll"
# Host-provided assemblies (shipped inside the base engine image) — never bundled into a plugin
# folder. Everything else in the publish output is treated as a plugin-private dependency.
SHARED_REGEX='^(DevC\.KPI\.|LinqCube|Dapper|Npgsql|Microsoft\.|System\.|YamlDotNet|netstandard|Azure\.|Newtonsoft\.)'

echo "==> Bundling plugin '$PLUGIN' (tenant '$TENANT')"

# --- publish the plugin -----------------------------------------------------------------------
PUB="$(mktemp -d)"
trap 'rm -rf "$PUB"' EXIT
echo "==> dotnet publish (Release)…"
dotnet publish "$PROJ" -c Release -o "$PUB" --nologo -clp:ErrorsOnly

# --- assemble the bundle ----------------------------------------------------------------------
rm -rf "$OUT"
mkdir -p "$OUT/plugins/$PLUGIN" "$OUT/config" "$OUT/secrets"

[ -f "$PUB/$PLUGIN_DLL" ] || { echo "ERROR: expected $PLUGIN_DLL in publish output" >&2; exit 1; }

private_count=0
for f in "$PUB"/*.dll; do
  base="$(basename "$f")"
  if [ "$base" = "$PLUGIN_DLL" ]; then
    cp "$f" "$OUT/plugins/$PLUGIN/"
  elif printf '%s' "$base" | grep -qE "$SHARED_REGEX"; then
    :   # host-provided (base image) — skip
  else
    cp "$f" "$OUT/plugins/$PLUGIN/"
    echo "    + private dependency: $base"
    private_count=$((private_count + 1))
  fi
done
# deps.json lets the load context resolve any private deps; harmless when there are none.
cp "$PUB/DevC.KPI.Plugins.$PLUGIN.deps.json" "$OUT/plugins/$PLUGIN/" 2>/dev/null || true

# --- config + secrets template ----------------------------------------------------------------
if [ -d "$CONFIG_DIR" ]; then
  cp -r "$CONFIG_DIR" "$OUT/config/$TENANT"
  echo "    config: config/$TENANT ($(find "$CONFIG_DIR" -type f | wc -l | tr -d ' ') files)"
else
  rmdir "$OUT/config" 2>/dev/null || true
  echo "    config: none (no config/$TENANT — Shared/example plugin?)"
fi
if [ -f "$SECRET_EXAMPLE" ]; then
  cp "$SECRET_EXAMPLE" "$OUT/secrets/"
  echo "    secrets: $TENANT.example.json (fill in per install; real secrets are never bundled)"
else
  rmdir "$OUT/secrets" 2>/dev/null || true
fi

# --- manifest ---------------------------------------------------------------------------------
GIT_SHA="$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)"
{
  echo "plugin:  $PLUGIN"
  echo "tenant:  $TENANT"
  echo "commit:  $GIT_SHA"
  echo "private-deps: $private_count"
  echo "built-from: $PROJ"
  echo "layout: drop plugins/ + config/ + secrets/ into the deployment volume; restart api."
} > "$OUT/BUNDLE-INFO.txt"

echo "==> Bundle ready: $OUT"
( cd "$OUT" && find . -type f | sed 's|^\./|    |' )

# --- optional archive -------------------------------------------------------------------------
if [ "$ARCHIVE" = "1" ]; then
  TARBALL="$(dirname "$OUT")/$PLUGIN-bundle.tar.gz"
  tar -czf "$TARBALL" -C "$(dirname "$OUT")" "$(basename "$OUT")"
  echo "==> Archive: $TARBALL"
fi
