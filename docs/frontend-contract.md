# Contrato de integração — Frontend Joya

Este documento é o acordo entre o frontend React/TypeScript e a API ASP.NET Core. Antes de considerar a integração concluída, confira se os DTOs do backend entregam os campos abaixo. ASP.NET Core com serialização JSON camelCase atende aos nomes esperados pelo React.

## Convenções

- Prefixo usado pelo browser: `/api`.
- Rotas reais esperadas pelo frontend, sem o prefixo: `/pedidos`, `/clientes`, `/produtos` e `/relatorios/top-clientes`.
- Datas de filtro: `YYYY-MM-DD`.
- Moeda: números decimais em reais; o frontend apenas formata o valor.
- IDs: números inteiros.
- Erros: preferencialmente `ProblemDetails` (`title`, `detail`) ou ValidationProblemDetails (`errors`).
- A API é a autoridade para preço, total do pedido e estoque. O frontend não envia preço no comando de criação.

## 1. GET /pedidos

Query string:

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---:|---|
| `clienteId` | int | Não | Filtra pedidos de um cliente. |
| `dataInicio` | date | Não | Data inicial inclusiva. |
| `dataFim` | date | Não | Data final inclusiva. |
| `pagina` | int | Sim | Página iniciando em 1. |
| `tamanhoPagina` | int | Sim | Quantidade por página; frontend envia 10. |

Resposta `200 OK`:

```json
{
  "items": [
    {
      "id": 42,
      "clienteId": 7,
      "clienteNome": "Maria Santos",
      "dataPedido": "2026-11-12T10:30:00Z",
      "quantidadeItens": 3,
      "valorTotal": 1250.50
    }
  ],
  "pagina": 1,
  "tamanhoPagina": 10,
  "totalRegistros": 1,
  "totalPaginas": 1
}
```

`items` deve ser um array vazio quando não houver resultados. `totalPaginas` pode ser zero quando `totalRegistros` for zero.

## 2. POST /pedidos

Request `application/json`:

```json
{
  "clienteId": 7,
  "itens": [
    { "produtoId": 2, "quantidade": 3 },
    { "produtoId": 5, "quantidade": 1 }
  ]
}
```

Resposta `201 Created` com `Location: /pedidos/{id}`:

```json
{
  "id": 43,
  "dataPedido": "2026-11-12T11:15:00Z",
  "valorTotal": 875.00
}
```

Regras de backend obrigatórias:

- Cliente precisa existir.
- A lista de itens não pode estar vazia.
- Quantidades devem ser inteiros maiores que zero.
- Produtos devem existir e ter estoque suficiente.
- O backend busca o preço atual no banco, calcula os subtotais e o total e atualiza o estoque dentro de uma transação.
- Não confiar em total, preço ou estoque informado pelo browser.
- Quantidades repetidas do mesmo produto são somadas antes da validação de estoque.

Erros usam `ProblemDetails`: pedido sem itens ou quantidade inválida retorna `400 Bad Request`; cliente ou produto inexistente retorna `404 Not Found`; estoque insuficiente retorna `409 Conflict`. A baixa de estoque, o pedido e seus itens são confirmados ou revertidos juntos.

## 3. GET /pedidos/{id}

Retorna o pedido criado, e é o destino do cabeçalho `Location` do POST.

Resposta `200 OK`: usa os mesmos campos de um item de `GET /pedidos`. Pedido inexistente retorna `404 Not Found`.

## 4. GET /clientes — endpoint adicional necessário ao formulário

O desafio não lista este endpoint, mas o formulário precisa de clientes cadastrados para preencher o select.

Resposta `200 OK`:

```json
[
  { "id": 7, "nome": "Maria Santos" },
  { "id": 8, "nome": "João Lima" }
]
```

Retornar apenas clientes disponíveis para novos pedidos, caso exista regra de ativo/inativo. Se o backend optar por paginação, adapte `lookupsApi.ts` ou mantenha esse endpoint simples para o escopo do teste.

## 5. GET /produtos — endpoint adicional necessário ao formulário

O desafio não lista este endpoint, mas o formulário precisa consultar preço e estoque antes da inclusão dos itens.

Resposta `200 OK`:

```json
[
  {
    "id": 2,
    "nome": "Teclado mecânico",
    "precoUnitario": 299.90,
    "estoqueDisponivel": 18
  },
  {
    "id": 5,
    "nome": "Mouse sem fio",
    "precoUnitario": 149.90,
    "estoqueDisponivel": 0
  }
]
```

O campo `estoqueDisponivel` é informativo para a interface. O backend precisa validar novamente no POST para evitar condição de corrida.

## 6. GET /relatorios/top-clientes

Query string:

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---:|---|
| `dataInicio` | date | Sim | Início inclusivo do período. |
| `dataFim` | date | Sim | Fim inclusivo do período. |

Resposta `200 OK` (máximo de cinco registros):

```json
[
  {
    "clienteId": 7,
    "clienteNome": "Maria Santos",
    "totalComprado": 12500.50,
    "quantidadePedidos": 8
  }
]
```

O backend deve ordenar por `totalComprado` em ordem decrescente. O período deve ser aplicado sem usar `YEAR(DataPedido)` sobre a coluna, para permitir o uso do índice por data.

## Mapeamento no código React

| Arquivo | Responsabilidade |
|---|---|
| `src/api/http.ts` | URL base, fetch e tratamento de erro. |
| `src/features/pedidos/pedidosApi.ts` | Listar e criar pedidos. |
| `src/features/pedidos/lookupsApi.ts` | Consultar clientes e produtos. |
| `src/features/relatorios/relatoriosApi.ts` | Consultar o ranking. |
| `src/types/api.ts` | DTOs/interfaces TypeScript do contrato. |


