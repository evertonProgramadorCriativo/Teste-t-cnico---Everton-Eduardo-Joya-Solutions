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
- `.claude/skills/new-endpoint/`: orientações para criar endpoints coerentes com o contrato e os padrões do backend.

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
docker compose build backend
docker compose --profile test run --build --rm backend-tests
```

O SDK .NET não precisa estar instalado no host: o build roda no estágio SDK do
`backend/Dockerfile`. Os testes unitários também rodam no container SDK e usam
SQLite em memória, sem iniciar o SQL Server.

Integração final:

```bash
docker compose up --build
```

## Claude Code / MCP

O projeto registra o MCP SQL Server em `.mcp.json`, usando DBHub em modo somente leitura no endpoint HTTP local `http://localhost:8081/mcp`. O hook `PostToolUse` executa `docker compose build backend` após alterações em arquivos do backend; o hook de typecheck do frontend continua ativo. As instruções da skill `new-endpoint` ficam em `.claude/skills/new-endpoint/SKILL.md`.

O Compose passa `SQL_SA_PASSWORD` do `.env` local, que não deve ser versionado, ao container DBHub. O MCP só publica a ferramenta SQL em modo somente leitura e a porta fica vinculada a `127.0.0.1`. Aprove o servidor SQL Server quando o Claude Code solicitar e confira o estado com `/mcp` ou `claude mcp list`. Nunca registre que o MCP executou uma consulta sem observar o resultado da ferramenta.
