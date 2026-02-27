#!/bin/bash
# Helper para rodar testes E2E localmente (sem Evolution API)
# Uso: ./scripts/run-e2e-tests.sh

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
E2E_CONNECTION="Host=localhost;Port=5433;Database=weeklyup_e2e;Username=weeklyup;Password=weeklyup123"

echo "Iniciando infraestrutura E2E..."
docker compose -f "$REPO_ROOT/docker-compose.e2e.yml" up -d --wait

echo "Rodando testes E2E (excluindo WhatsApp)..."
TEST_DB_CONNECTION="$E2E_CONNECTION" \
  dotnet test "$REPO_ROOT/tests/WeeklyUp.Api.Tests" \
    --filter "Category=E2E&Category!=WhatsApp" \
    --logger "console;verbosity=normal"
E2E_EXIT_CODE=$?

echo "Derrubando infraestrutura E2E..."
docker compose -f "$REPO_ROOT/docker-compose.e2e.yml" down

exit $E2E_EXIT_CODE
