---
name: new-endpoint
description: Criar ou ajustar endpoint ASP.NET Core no padrão do Mini Gestão de Pedidos.
---

# Novo endpoint no padrão do projeto

1. Leia `CLAUDE.md`, `docs/spec.md` e `docs/frontend-contract.md`; confirme método HTTP, rota, request, response e erros esperados.
2. Siga a organização e os padrões já existentes no backend. Mantenha regras de negócio em serviços testáveis, não em handlers/controllers.
3. Use tipos explícitos, nomes públicos em PascalCase e métodos assíncronos com sufixo `Async`. Propague `CancellationToken` nas operações de I/O.
4. Use consultas parametrizadas e não confie em preço, total, estoque ou identificadores enviados pelo cliente sem validá-los no backend.
5. Retorne códigos HTTP e `ProblemDetails` coerentes com o contrato. Não esconda erros de banco nem retorne sucesso como fallback.
6. Atualize os DTOs e `docs/frontend-contract.md` quando o contrato mudar; ajuste também os tipos e chamadas do frontend se forem afetados.
7. Acrescente testes focados para o comportamento novo ou corrigido, incluindo erros relevantes.
8. Valide o backend com `docker compose build backend` (o SDK .NET é executado dentro da imagem Docker) e execute os testes aplicáveis.
9. Resuma as alterações e informe os comandos de validação realmente executados.
