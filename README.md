# Joya Solutions — Mini Gestão de Pedidos

Starter de frontend para o teste técnico Full Stack da Joya Solutions. O projeto foi organizado para entrar em um monorepositório com o backend .NET e o SQL Server.

## Bloco 1 — API e SQL Server no Docker

Requisitos: Docker Desktop com Docker Compose.

1. Copie `.env.example` para `.env` e defina `SQL_SA_PASSWORD` com uma senha local forte que atenda aos requisitos do SQL Server. O arquivo `.env` é ignorado pelo Git.
2. Na raiz do projeto, execute:

   ```bash
   docker compose up --build
   ```

3. Verifique a API em `http://localhost:8080/health`. Quando o SQL Server estiver acessível, a resposta será:

   ```json
   { "status": "ok", "banco": "conectado", "clientes": 3, "produtos": 4 }
   ```

O Compose aguarda o SQL Server ficar saudável, executa `database/01-schema.sql` e `database/02-seed.sql` em um serviço de inicialização e só então inicia a API. Os scripts são idempotentes e podem ser executados novamente sem duplicar os dados de exemplo. O health endpoint abre uma conexão real com o banco `JoyaPedidos` e retorna as contagens atuais de clientes e produtos. O SQL Server persiste dados no volume `sqlserver-data` e fica disponível na porta `1433`.

## Bloco 2 — Banco de dados

Os scripts em `database/` criam `Clientes`, `Produtos`, `Pedidos` e `ItensPedido`, com chaves primárias/estrangeiras, restrições e índices. `01-schema.sql` cria o banco e as tabelas; `02-seed.sql` carrega três clientes e quatro produtos de exemplo. Ambos podem ser executados novamente sem recriar as tabelas nem duplicar os dados de exemplo.

O Compose executa os scripts automaticamente antes de iniciar a API. Para conferir as contagens no banco `JoyaPedidos`, execute:

```sql
SELECT
    (SELECT COUNT(*) FROM dbo.Clientes) AS Clientes,
    (SELECT COUNT(*) FROM dbo.Produtos) AS Produtos,
    (SELECT COUNT(*) FROM dbo.Pedidos) AS Pedidos,
    (SELECT COUNT(*) FROM dbo.ItensPedido) AS ItensPedido;
```

Após a carga inicial, os valores verificados via MCP foram **3 clientes, 4 produtos, 0 pedidos e 0 itens**. O endpoint `GET /health` também confirma a conexão e retorna as contagens de clientes e produtos.

## Bloco 3 — Configuração AI First e MCP

- As regras do backend estão em [`CLAUDE.md`](CLAUDE.md); a skill de endpoint está em [`.claude/skills/new-endpoint/SKILL.md`](.claude/skills/new-endpoint/SKILL.md).
- O hook `PostToolUse` executa `docker compose build backend` quando Claude edita um arquivo dentro de `backend/`. O build usa o SDK da imagem Docker; não exige SDK .NET instalado no host. O hook de typecheck do frontend continua configurado.
- [`.mcp.json`](.mcp.json) registra o DBHub como MCP HTTP local em `http://localhost:8081/mcp`. O serviço é iniciado pelo Compose, e [`.claude/dbhub.toml`](.claude/dbhub.toml) limita a ferramenta `execute_sql` a consultas somente leitura.
- O Compose passa a senha `SQL_SA_PASSWORD` de `.env` ao container DBHub; não coloque senhas no JSON, TOML ou Git. A porta MCP está vinculada somente a `127.0.0.1`. Aprove o servidor SQL Server no Claude Code e verifique seu estado com `/mcp` ou `claude mcp list`.

Para testar o fluxo em uma sessão Claude Code autenticada, abra o projeto, aprove o MCP SQL Server e confirme sua conexão com `/mcp`. Depois pergunte: “Quantos clientes existem? Consulte o SQL Server via MCP.” A consulta esperada é `SELECT COUNT(*) AS TotalClientes FROM dbo.Clientes` e, após a carga inicial, deve retornar `3`.

**Uso do MCP nesta etapa:** Claude Code listou o servidor como conectado. A ferramenta MCP `execute_sql` executou a contagem de clientes e a contagem das quatro tabelas: 3 clientes, 4 produtos, 0 pedidos e 0 itens. A tentativa de pedir a resposta em linguagem natural via `claude -p` não foi concluída porque o Claude CLI não estava autenticado (`Not logged in`); portanto, não afirmamos que Claude Code respondeu à pergunta em linguagem natural nesta sessão.

## Bloco 4 — Endpoints de leitura e erros

- `GET http://localhost:8080/clientes` retorna clientes ordenados por ID.
- `GET http://localhost:8080/produtos` retorna produtos com preço e estoque atuais.
- Ambos consultam o SQL Server via EF Core e retornam JSON camelCase.
- A API usa middleware global para responder erros e rotas inexistentes com `ProblemDetails`.
- As requisições para Postman ou a extensão REST Client estão em [`requests.http`](requests.http).

No Postman, envie `GET http://localhost:8080/clientes` e `GET http://localhost:8080/produtos`; ambas devem responder `200 OK`, com 3 clientes e 4 produtos após a carga inicial. Para conferir o tratamento de rota inexistente, envie `GET http://localhost:8080/rota-inexistente`; o esperado é `404 Not Found`, `Content-Type: application/problem+json` e um corpo ProblemDetails.

## Bloco 5 — Frontend no Docker

O serviço `frontend` publica o Nginx na porta `3000` e encaminha `/api/*` para o serviço `backend:8080`. Suba o projeto inteiro na raiz:

```bash
docker compose up --build
```

Abra `http://localhost:3000`, clique em **Novo pedido** e confira se os selects de cliente e produto são preenchidos. No Postman, valide também o proxy:

- `GET http://localhost:3000/api/clientes` — `200 OK`, lista de clientes.
- `GET http://localhost:3000/api/produtos` — `200 OK`, produtos com `precoUnitario` e `estoqueDisponivel`.

`GET /pedidos` retorna uma página vazia até que existam pedidos. O `POST /pedidos` cria pedidos calculando preços no backend e baixando o estoque em uma transação.

Para criar um pedido pelo Postman, envie `POST http://localhost:3000/api/pedidos` com `Content-Type: application/json`:

```json
{
  "clienteId": 1,
  "itens": [
    { "produtoId": 1, "quantidade": 1 }
  ]
}
```

Uma criação válida retorna `201 Created`; consulte os exemplos de sucesso e validação em [`requests.http`](requests.http). O backend retorna `400` para itens ou quantidades inválidas, `404` para cliente/produto inexistente e `409` para estoque insuficiente. Atualize os dados da tela após salvar para conferir o pedido na listagem.

## Bloco 6 — Listagem de pedidos

`GET /pedidos` aceita os filtros opcionais `clienteId`, `dataInicio` e `dataFim`, além de `pagina` (a partir de 1) e `tamanhoPagina` (de 1 a 50). O período inclui as datas inicial e final. A resposta segue o contrato `{ items, pagina, tamanhoPagina, totalRegistros, totalPaginas }`; quando não há resultados, `items` é vazio e `totalPaginas` é zero.

No Postman, use `http://localhost:8080/pedidos` para testar sem filtro, por cliente, por período, página 2 e um filtro sem resultados. Também é possível validar o proxy em `http://localhost:3000/api/pedidos`. Exemplos estão em [`requests.http`](requests.http). Na tela em `http://localhost:3000`, a tabela exibe a página atual e os filtros de cliente e período; os controles de paginação ficam desabilitados quando só há uma página.

## Bloco 7 — Criação de pedidos

`POST /pedidos` recebe `clienteId` e itens com `produtoId` e `quantidade`. O backend valida cliente, itens, produtos e estoque, calcula o preço com os valores do banco e persiste o pedido, os itens e a baixa do estoque na mesma transação. Uma resposta válida é `201 Created`; os erros esperados são `400`, `404` ou `409`, em formato `ProblemDetails`.

No Postman, execute os seis exemplos de criação em [`requests.http`](requests.http): sucesso, cliente inexistente, sem itens, quantidade zero, produto inexistente e estoque insuficiente. Após o sucesso, use `GET /produtos` para confirmar a redução do estoque e `GET /pedidos` para localizar o pedido. A validação do modal pode impedir quantidades acima do estoque antes de chamar a API; para testar o `409` do backend, envie diretamente o caso de estoque insuficiente pelo Postman.

## Bloco 8 — Top clientes e debug da query

`GET /relatorios/top-clientes?dataInicio=YYYY-MM-DD&dataFim=YYYY-MM-DD` retorna até cinco clientes, ordenados pelo total comprado em ordem decrescente. O intervalo inclui `dataFim`, convertida no backend para o limite exclusivo do dia seguinte. O painel usa essa rota pelo proxy `/api`.

O benchmark reproduzível em `database/03-query-benchmark.sql` cria 1.000 clientes (50 sem pedidos), 34.200 pedidos e 68.400 itens temporários dentro de uma transação. Execute-o em ambiente local sem outros escritores concorrentes; ao final, os dados são revertidos e as identidades voltam ao maior ID persistido. Para capturar planos reais e `STATISTICS IO` no SQL Server do Compose:

```powershell
docker compose run --rm --no-deps --entrypoint /bin/bash database-init -lc '/opt/mssql-tools18/bin/sqlcmd -S sqlserver,1433 -U sa -P "$MSSQL_SA_PASSWORD" -d JoyaPedidos -C -b -i /scripts/03-query-benchmark.sql -y 0 -Y 0'
```

O script imprime o plano de execução real e as leituras lógicas antes (query original) e depois (query corrigida). Resultados observados em uma execução local:

**Antes:**

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total;
```

**Depois:**

```sql
SELECT TOP (5)
    c.Id AS ClienteId,
    c.Nome AS ClienteNome,
    SUM(p.ValorTotal) AS TotalComprado,
    COUNT(p.Id) AS QuantidadePedidos
FROM dbo.Pedidos AS p
INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
WHERE p.DataPedido >= @DataInicio
  AND p.DataPedido < @DataFimExclusivo
GROUP BY c.Id, c.Nome
ORDER BY TotalComprado DESC, c.Id ASC;
```

| Métrica | Antes | Depois |
|---|---:|---:|
| Leituras lógicas — `Pedidos` | 95 | 48 |
| Leituras lógicas — `Clientes` | 16 | 10 |
| Leituras lógicas — `ItensPedido` | 290 | 0 |
| Leituras lógicas — `Worktable` | 0 | 0 |
| Total de leituras lógicas | 401 | 58 |
| CPU da execução | 97 ms | 8 ms |
| Tempo decorrido da execução | 93 ms | 9 ms |

O plano anterior fez `Index Scan` em `IX_Pedidos_ClienteId_DataPedido` e leituras em `ItensPedido`. O plano corrigido fez `Index Seek` em `IX_Pedidos_DataPedido`, seguido de `Clustered Index Seek` em `PK_Clientes`, sem acessar `ItensPedido`. O índice por data já existia desde o Bloco 2 e cobre `ClienteId` e `ValorTotal`.

Correções aplicadas: o `WHERE YEAR(p.DataPedido) = 2026` tornava o filtro não sargable; foi substituído por limites `>= @dataInicio` e `< @dataFimExclusivo`. O filtro no lado nullable do `LEFT JOIN` eliminava as linhas sem pedido, comportando-se como `INNER JOIN`; a consulta agora usa `INNER JOIN` explícito, apropriado para um ranking de quem comprou. A ordenação implícita era crescente e não havia limite; agora usa `TOP (5)` e `DESC`. Agrupar somente pelo nome juntava homônimos; agora o agrupamento inclui `ClienteId`.

Além disso, a consulta do relatório soma `Pedidos.ValorTotal`, gravado pelo backend na mesma transação que os itens, evitando a junção e leitura de `ItensPedido`. Os totais do benchmark são gerados a partir dos mesmos preços e quantidades dos itens. Os tempos variam conforme cache e ambiente; nesta execução as leituras físicas foram zero, então use os números como evidência local, não como garantia de produção.

## Bloco 9 — Testes unitários do serviço de pedidos

Os testes de `PedidoCreationService` cobrem cliente inexistente, pedido sem itens, quantidade zero ou negativa, produto inexistente, estoque insuficiente com rollback, cálculo do total usando o preço do banco, produto repetido e baixa correta do estoque. Eles usam SQLite em memória para exercitar as transações e `ExecuteUpdateAsync` do EF Core sem depender do SQL Server ou de mocks.

Para executar os testes dentro do container com o SDK .NET, na raiz do repositório, rode:

```powershell
docker compose --profile test run --build --rm backend-tests
```

O serviço `backend-tests` fica no perfil `test`, portanto não é iniciado pelo `docker compose up` normal. O teste não sobe o SQL Server nem altera os dados do volume da aplicação.

## O que já está implementado

- Tela de listagem de pedidos com filtro por cliente e período.
- Paginação controlada pela API.
- Modal para criar pedido com um ou mais produtos.
- Cálculo visual do total e validações de quantidade/estoque no cliente.
- Consulta do ranking dos cinco clientes com maior valor comprado no período.
- Estados de carregamento, erro e lista vazia.
- Layout responsivo para desktop e celular.
- Dockerfile de produção com Nginx e proxy `/api` para o serviço `backend`.
- Contrato esperado da API documentado em [`docs/frontend-contract.md`](docs/frontend-contract.md).
- Instruções para Claude Code em [`CLAUDE.md`](CLAUDE.md).

> Importante: o teste descreve explicitamente `POST /pedidos`, `GET /pedidos` e `GET /relatorios/top-clientes`. Para preencher os selects do cadastro, este frontend também espera `GET /clientes` e `GET /produtos`; esses endpoints já retornam os formatos descritos no contrato.

## Requisitos

- Node.js 22 ou superior (recomendado).
- npm 10 ou superior.
- Backend .NET executando em `http://localhost:8080` durante o desenvolvimento local.

## Executar localmente

No terminal, dentro da pasta `frontend`:

```bash
cp .env.example .env
npm install
npm run dev
```

Abra `http://localhost:5173`.

O Vite encaminha as chamadas `/api/*` para `http://localhost:8080/*`. Assim, o frontend não precisa conhecer o hostname interno do container do backend. Caso a API esteja em outra porta, ajuste `target` em `vite.config.ts`.

## Build e verificação de tipos

```bash
npm run typecheck
npm run build
npm run preview
```

## Docker

O `frontend/Dockerfile` compila a aplicação e publica os arquivos por Nginx. O Nginx encaminha `/api/*` para `http://backend:8080/*`.

O `docker-compose.yml` da raiz sobe SQL Server, inicialização do banco, DBHub, backend e frontend com `docker compose up --build`. A interface fica disponível em `http://localhost:3000` e a API em `http://localhost:8080`.

## Contrato da API

Consulte [`docs/frontend-contract.md`](docs/frontend-contract.md) antes de implementar os controllers/DTOs .NET. O contrato define nomes de propriedades, query strings, paginação e payload do `POST /pedidos`.


