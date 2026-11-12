---
name: frontend-feature
description: Implementar ou ajustar uma funcionalidade no frontend React/TypeScript do Mini Gestão de Pedidos mantendo o contrato da API.
---

# Skill: implementar funcionalidade no frontend

Ao receber uma tarefa de frontend:

1. Leia `CLAUDE.md` e `docs/frontend-contract.md`.
2. Identifique a feature correspondente em `frontend/src/features/` e a página/componente que a utiliza.
3. Preserve a separação: componentes visuais não devem montar URLs nem duplicar `fetch`; chamadas ficam nos arquivos `*Api.ts` usando `src/api/http.ts`.
4. Atualize `src/types/api.ts` quando o contrato mudar e atualize `docs/frontend-contract.md` na mesma alteração.
5. Trate loading, erro, vazio e sucesso. Não use dados mockados como fallback silencioso.
6. Valide entradas no frontend para UX, mas mantenha as regras definitivas no backend.
7. Não envie preço ou total calculado pelo navegador no comando de criação do pedido.
8. Execute `npm run typecheck` e `npm run build` dentro de `frontend/`.
9. Resuma arquivos alterados, decisões e verificações executadas. Não declare testes que não foram executados.
