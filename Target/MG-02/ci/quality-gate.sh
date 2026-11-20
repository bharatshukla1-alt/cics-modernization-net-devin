#!/usr/bin/env bash
# MG-02 quality gate (CQ-005): build, tests with coverage thresholds, lint, dependency audit.
# Wire this into the repository pipeline; it exits non-zero on any failure.
# API integration tests need ConnectionStrings__SignOn (the read-only carddemo_signon login) pointing at the
# provisioned PostgreSQL database with the MG-02 usrsec table.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MIN_BACKEND_LINE_COVERAGE="${MIN_BACKEND_LINE_COVERAGE:-80}"

echo "== Backend: build (warnings as errors)"
cd "$ROOT/Backend/SignOnService"
dotnet build -warnaserror
echo "== Backend: tests + coverage"
rm -rf TestResults
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
python3 - "$MIN_BACKEND_LINE_COVERAGE" <<'PY'
import glob, sys, xml.etree.ElementTree as ET
reports = glob.glob("TestResults/**/coverage.cobertura.xml", recursive=True)
if not reports:
    sys.exit("No coverage report produced")
rate = float(ET.parse(reports[0]).getroot().get("line-rate")) * 100
print(f"Backend line coverage: {rate:.1f}% (minimum {sys.argv[1]}%)")
sys.exit(0 if rate >= float(sys.argv[1]) else 1)
PY
echo "== Backend: vulnerable packages"
if dotnet list package --vulnerable --include-transitive | grep -q "has the following vulnerable packages"; then
  echo "Vulnerable NuGet packages found"; exit 1
fi

echo "== Frontend: install, lint, tests + coverage thresholds, build, audit"
cd "$ROOT/Frontend"
npm ci
npm run lint
npm run coverage
npm run build
npm audit --omit=dev --audit-level=high

echo "MG-02 quality gate passed"
