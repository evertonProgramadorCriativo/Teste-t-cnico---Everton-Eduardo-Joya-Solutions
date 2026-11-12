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

> Importante: o teste descreve explicitamente `POST /pedidos`, `GET /pedidos` e `GET /relatorios/top-clientes`. Para preencher os selects do cadastro, este frontend também espera `GET /clientes` e `GET /produtos`. Esses dois endpoints adicionais estão documentados e precisam ser implementados/alinhados no backend antes da integração final.

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

No `docker-compose.yml` da raiz do monorepositório, mantenha o nome do serviço .NET como `backend` e a porta interna da API como `8080`, ou ajuste o destino em `frontend/nginx.conf`.

Exemplo de serviço para adicionar ao Compose da raiz:

```yaml
frontend:
  build:
    context: ./frontend
    dockerfile: Dockerfile
  ports:
    - "3000:80"
  depends_on:
    - backend
```

Depois de integrar o backend e o SQL Server, a aplicação poderá ser iniciada pela raiz com `docker compose up --build`.

## Contrato da API

Consulte [`docs/frontend-contract.md`](docs/frontend-contract.md) antes de implementar os controllers/DTOs .NET. O contrato define nomes de propriedades, query strings, paginação e payload do `POST /pedidos`.


