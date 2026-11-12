IF DB_ID(N'JoyaPedidos') IS NULL
BEGIN
    CREATE DATABASE JoyaPedidos;
END;
GO

USE JoyaPedidos;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        Nome NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        CONSTRAINT UQ_Clientes_Email UNIQUE (Email)
    );
END;
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Produtos PRIMARY KEY,
        Nome NVARCHAR(150) NOT NULL,
        PrecoUnitario DECIMAL(18,2) NOT NULL,
        EstoqueDisponivel INT NOT NULL,
        CONSTRAINT CK_Produtos_PrecoUnitario CHECK (PrecoUnitario >= 0),
        CONSTRAINT CK_Produtos_EstoqueDisponivel CHECK (EstoqueDisponivel >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pedidos PRIMARY KEY,
        ClienteId INT NOT NULL,
        DataPedido DATETIME2 NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
        CONSTRAINT CK_Pedidos_ValorTotal CHECK (ValorTotal >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ItensPedido PRIMARY KEY,
        PedidoId INT NOT NULL,
        ProdutoId INT NOT NULL,
        Quantidade INT NOT NULL,
        PrecoUnitario DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id),
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
BEGIN
    CREATE INDEX IX_Pedidos_DataPedido
        ON dbo.Pedidos (DataPedido)
        INCLUDE (ClienteId, ValorTotal);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ClienteId_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
BEGIN
    CREATE INDEX IX_Pedidos_ClienteId_DataPedido
        ON dbo.Pedidos (ClienteId, DataPedido);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_PedidoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
BEGIN
    CREATE INDEX IX_ItensPedido_PedidoId ON dbo.ItensPedido (PedidoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_ProdutoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
BEGIN
    CREATE INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);
END;
GO
