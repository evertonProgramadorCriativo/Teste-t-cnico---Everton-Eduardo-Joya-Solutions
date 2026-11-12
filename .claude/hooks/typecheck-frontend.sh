#!/bin/sh
set -eu

# Ignora o hook antes da instalação das dependências.
if [ ! -d "frontend/node_modules" ]; then
  echo "[hook] frontend/node_modules não existe; pulei o typecheck. Rode npm install em frontend/."
  exit 0
fi

cd frontend
npm run typecheck
