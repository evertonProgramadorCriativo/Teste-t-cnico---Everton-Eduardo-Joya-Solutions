# Especificação — Mini Gestão de Pedidos

## 1. Objetivo

Construir uma aplicação web para cadastrar e consultar pedidos de clientes, com backend .NET 8, SQL Server e frontend React + TypeScript. O foco é demonstrar uma entrega full stack organizada, testável, conteinerizada e desenvolvida com apoio documentado de Claude Code.

A aplicação inteira deve subir com um único comando: `docker compose up --build`.

## 2. Escopo

### Dentro do escopo
1. Consultar pedidos com filtros por cliente e período, com paginação.
2. Cadastrar pedido com um cliente e um ou mais produtos.
3. Validar cliente, itens, quantidades e estoque no backend.
4. Consultar os cinco clientes com maior valor de pedidos em um período.
5. Persistir Clientes, Produtos, Pedidos e ItensPedido no SQL Server.
6. Corrigir e otimizar a query de ranking fornecida na Parte 4 do desafio.

### Fora do escopo
- Autenticação e autorização.
- Cadastro, edição ou exclusão de clientes e produtos pela API (a carga vem dos scripts em `database/`).
- Edição, cancelamento ou exclusão de pedidos.
- Pagamento, frete, impostos e descontos.

## 3. Regras de negócio

| Id | Regra | Resposta em caso de violação |
|---|---|---|
| RN-01 | Um pedido pertence a um cliente existente. | 404 |
| RN-02 | Um pedido deve ter ao menos um item. | 400 |
| RN-03 | Cada item referencia um produto existente. | 404 |
| RN-04 | A quantidade de cada item é um inteiro maior que zero. | 400 |
| RN-05 | Não é permitido vender mais do que o estoque disponível. | 409 |
| RN-06 | Preço, subtotais e total são calculados no backend a partir do preço atual do produto; o frontend não envia valores monetários. | n/a |
| RN-07 | Criação do pedido, dos itens e baixa de estoque ocorrem na mesma transação (tudo ou nada). | rollback |
| RN-08 | O mesmo produto repetido na lista de itens tem as quantidades somadas antes da validação de estoque. | n/a |
| RN-09 | O item guarda o preço unitário vigente no momento da compra (histórico). | n/a |
| RN-10 | A listagem aceita cliente e intervalo de datas opcionais; as datas são inclusivas. | n/a |
| RN-11 | O relatório retorna no máximo 5 clientes, ordenados pelo valor total comprado em ordem decrescente. | n/a |

## 4. Modelo de dados

### Clientes
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| Nome | NVARCHAR(150) | NOT NULL |
| Email | NVARCHAR(200) | NOT NULL, UNIQUE |

### Produtos
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| Nome | NVARCHAR(150) | NOT NULL |
| PrecoUnitario | DECIMAL(18,2) | NOT NULL, CHECK >= 0 |
| EstoqueDisponivel | INT | NOT NULL, CHECK >= 0 |

### Pedidos
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| ClienteId | INT | NOT NULL, FK → Clientes(Id) |
| DataPedido | DATETIME2 | NOT NULL, preenchida pelo backend |
| ValorTotal | DECIMAL(18,2) | NOT NULL, CHECK >= 0 |

### ItensPedido
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| PedidoId | INT | NOT NULL, FK → Pedidos(Id) |
| ProdutoId | INT | NOT NULL, FK → Produtos(Id) |
| Quantidade | INT | NOT NULL, CHECK > 0 |
| PrecoUnitario | DECIMAL(18,2) | NOT NULL, CHECK >= 0 |

### Índices
| Índice | Colunas | Motivo |
|---|---|---|
| IX_Pedidos_DataPedido | DataPedido (INCLUDE ClienteId, ValorTotal) | filtro por período e relatório |
| IX_Pedidos_ClienteId_DataPedido | ClienteId, DataPedido | filtro por cliente e período |
| IX_ItensPedido_PedidoId | PedidoId | junção Pedidos × Itens |
| IX_ItensPedido_ProdutoId | ProdutoId | integridade e consultas por produto |

`Pedidos.ValorTotal` é gravado no momento da criação para evitar recalcular a soma dos itens a cada listagem. Os itens preservam o preço do momento da compra.

## 5. Endpoints

Todos os JSON usam camelCase. Datas de filtro no formato `YYYY-MM-DD`. Erros seguem `ProblemDetails` (`title`, `detail`; `errors` para validação). Os formatos completos estão em [`frontend-contract.md`](frontend-contract.md).

| Método e rota | Descrição | Sucesso |
|---|---|---|
| `POST /pedidos` | Cria pedido com itens. | 201 `{ id, dataPedido, valorTotal }` |
| `GET /pedidos` | Lista com filtros `clienteId`, `dataInicio`, `dataFim` e paginação `pagina`, `tamanhoPagina`. | 200 `{ items, pagina, tamanhoPagina, totalRegistros, totalPaginas }` |
| `GET /relatorios/top-clientes` | Top 5 por valor total no período (`dataInicio` e `dataFim` obrigatórios). | 200 lista de até 5 itens |
| `GET /clientes` | Apoio ao formulário. | 200 `[{ id, nome }]` |
| `GET /produtos` | Apoio ao formulário (preço e estoque atuais). | 200 `[{ id, nome, precoUnitario, estoqueDisponivel }]` |
| `GET /health` | Verifica API e conexão com o banco, incluindo contagem de clientes e produtos. | 200 `{ status, banco, clientes, produtos }` |

### Códigos de erro
- **400**: dados inválidos (sem itens, quantidade ≤ 0, `dataInicio` > `dataFim`, página inválida).
- **404**: cliente ou produto inexistente.
- **409**: estoque insuficiente.

## 6. Decisões de projeto (a revisar no plano)

- **Estoque e concorrência:** baixa de estoque com `UPDATE Produtos SET EstoqueDisponivel = EstoqueDisponivel - @qtd WHERE Id = @id AND EstoqueDisponivel >= @qtd`, verificando as linhas afetadas dentro da transação. Se zero linhas, retorna 409 e faz rollback.
- **Paginação:** `pagina` começa em 1; `tamanhoPagina` máximo de 50; ordenação padrão por `DataPedido` decrescente e `Id` decrescente.
- **Intervalo de datas:** `DataPedido >= dataInicio AND DataPedido < dataFim + 1 dia`, sem função sobre a coluna indexada.
- **Relatório:** agrupa por `ClienteId` (não por nome), ordena por total decrescente e limita a 5.
- **Camadas:** controller → serviço de pedidos (regras) → acesso a dados (EF Core). As regras ficam no serviço para serem testáveis sem HTTP.
- **Fuso:** `DataPedido` gravada em UTC pelo backend.

## 7. Critérios de aceite

| Id | Critério |
|---|---|
| CA-01 | Dado pedidos no banco, `GET /pedidos` devolve a primeira página com `totalRegistros` e `totalPaginas` corretos. |
| CA-02 | Com `clienteId`, só pedidos daquele cliente aparecem; com `dataInicio` e `dataFim`, apenas os do período (extremos incluídos). |
| CA-03 | Com `pagina=2`, a API devolve o segundo bloco sem repetir itens da página 1. |
| CA-04 | `POST /pedidos` válido devolve 201, grava pedido e itens, calcula o total com preço do banco e reduz o estoque. |
| CA-05 | `POST /pedidos` com cliente inexistente devolve 404 e não grava nada. |
| CA-06 | `POST /pedidos` sem itens devolve 400. |
| CA-07 | `POST /pedidos` com quantidade zero ou negativa devolve 400. |
| CA-08 | `POST /pedidos` com produto inexistente devolve 404. |
| CA-09 | `POST /pedidos` com estoque insuficiente devolve 409 e o estoque de nenhum produto muda (rollback). |
| CA-10 | Após criar um pedido, ele aparece no topo da listagem. |
| CA-11 | `GET /relatorios/top-clientes` devolve no máximo 5 clientes em ordem decrescente de total. |
| CA-12 | O relatório respeita o período e não usa função sobre `DataPedido`. |
| CA-13 | `docker compose up --build` sobe SQL Server (scripts executados), backend e frontend sem ajuste manual. |
| CA-14 | Testes unitários e de integração (SQL Server real) passam com `dotnet test`. |
| CA-15 | O README explica decisões, execução, testes, uso do Claude Code e o debug da query. |
| CA-16 | `GET /health` confirma a conexão e retorna as quantidades de clientes e produtos carregados pelos scripts do banco. |

## 8. Pontos ainda sujeitos a decisão

- Estratégia concreta do MCP SQL Server e seu registro no ambiente Claude Code.
- Como executar o `dotnet` (build e testes) por container, já que não há SDK local.
- Formato de data devolvido pela API versus o esperado pelo frontend (conferir no Bloco 5).
