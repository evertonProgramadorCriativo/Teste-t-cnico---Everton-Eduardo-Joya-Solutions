# CLAUDE.md — Joya Mini Gestão de Pedidos

## Contexto do projeto

Este repositório é o starter de um monorepositório para o teste técnico Joya Solutions. O frontend já existe em `frontend/`. O backend .NET e o SQL Server ainda devem ser planejados e implementados neste mesmo repositório.

Antes de implementar o backend, leia:

- `docs/spec.md`
- `docs/frontend-contract.md`
- `README.md`

## Processo de trabalho obrigatório

1. Antes de alterar código de backend, use Plan Mode do Claude Code para analisar o desafio inteiro, as pastas existentes e o contrato frontend/backend.
2. Gere `docs/plan.md` a partir do plano efetivamente produzido no Plan Mode. Registre também ajustes, discordâncias e motivos; não invente histórico de decisões.
3. Implemente em etapas pequenas: banco/scripts, domínio/serviços, endpoints, testes, Docker Compose, integração frontend.
4. Não sobrescreva o frontend sem necessidade. Primeiro compare a implementação com o contrato documentado.
5. Ao terminar uma etapa, rode build/typecheck/testes relevantes e revise o diff.
6. Preserve commits pequenos e claros. Não faça squash/rebase para apagar a evolução.

## Arquitetura esperada

- `frontend/`: React + TypeScript + Vite; componentes por feature; chamadas HTTP centralizadas em `src/api` e `src/features/*/*Api.ts`.
- `backend/`: ASP.NET Core .NET 8 ou superior. Separe responsabilidades de API, aplicação/domínio e persistência de acordo com o tamanho do desafio; evite abstrações desnecessárias.
- `database/`: scripts idempotentes de criação, índices e carga de dados demonstrativos.
- `docs/`: especificação, plano, contrato de integração e documentação técnica.

## Convenções de código

- Escreva em português para documentação e mensagens de interface; use nomes de código claros e consistentes.
- React: componentes em PascalCase; funções/variáveis em camelCase; interfaces TypeScript em PascalCase.
- .NET: tipos/propriedades/métodos públicos em PascalCase; variáveis/parâmetros em camelCase; async methods com sufixo `Async`.
- Não duplique contratos: se o DTO mudar, atualize `docs/frontend-contract.md` e as interfaces TypeScript.
- Não coloque preço, total ou validação definitiva de estoque sob autoridade do frontend.
- Trate datas e intervalos com limites inclusivos/exclusivos corretos e sem função sobre a coluna indexada.
- Nunca coloque segredos, senhas ou connection strings reais no Git.

## Contrato do frontend

O frontend usa `/api` no browser. O Vite remove o prefixo e encaminha para `http://localhost:8080` em desenvolvimento. No Docker, `frontend/nginx.conf` encaminha para `http://backend:8080`.

Endpoints consumidos:

- `GET /pedidos?clienteId=&dataInicio=&dataFim=&pagina=1&tamanhoPagina=10`
- `POST /pedidos`
- `GET /clientes` (complementar, necessário ao select)
- `GET /produtos` (complementar, necessário ao select)
- `GET /relatorios/top-clientes?dataInicio=YYYY-MM-DD&dataFim=YYYY-MM-DD`

Consulte `docs/frontend-contract.md` para formatos exatos de request/response.

## Comandos

Frontend:

```bash
cd frontend
npm install
npm run typecheck
npm run build
npm run dev
```

Backend (ajustar à solução criada):

```bash
dotnet restore
dotnet build
dotnet test
```

Integração final:

```bash
docker compose up --build
```

## Claude Code / MCP

O desafio exige MCP conectado ao SQL Server. Configure o servidor MCP no ambiente local do Claude Code usando credenciais de desenvolvimento fora do Git. Não invente que o MCP foi conectado nem que executou consultas se isso não aconteceu. Documente no README final quais operações foram realmente realizadas pelo MCP.
