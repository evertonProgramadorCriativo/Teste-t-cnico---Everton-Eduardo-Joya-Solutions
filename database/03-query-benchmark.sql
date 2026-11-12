SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @BenchmarkStart DATETIME2(0) = '20250101';
DECLARE @BenchmarkEnd DATETIME2(0) = '20280101';
DECLARE @DataInicio DATETIME2(0) = '20260101';
DECLARE @DataFimExclusivo DATETIME2(0) = '20270101';
DECLARE @PrecoProduto1 DECIMAL(18, 2);
DECLARE @PrecoProduto2 DECIMAL(18, 2);

DECLARE @BenchmarkClientes TABLE
(
    ClienteId INT NOT NULL PRIMARY KEY,
    Email NVARCHAR(200) NOT NULL
);

DECLARE @Produtos TABLE
(
    Ordem INT NOT NULL PRIMARY KEY,
    ProdutoId INT NOT NULL,
    PrecoUnitario DECIMAL(18, 2) NOT NULL
);

DECLARE @Anos TABLE (Ano INT NOT NULL PRIMARY KEY);
DECLARE @Meses TABLE (Mes INT NOT NULL PRIMARY KEY);

BEGIN TRY
    BEGIN TRANSACTION;

    ;WITH Numeros AS
    (
        SELECT TOP (1000)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Numero
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO dbo.Clientes (Nome, Email)
    OUTPUT inserted.Id, inserted.Email
        INTO @BenchmarkClientes (ClienteId, Email)
    SELECT
        CASE
            WHEN Numero IN (1, 2) THEN N'Benchmark Homônimo'
            ELSE CONCAT(N'Benchmark Cliente ', Numero)
        END,
        CONCAT(N'bench-', Numero, N'@example.invalid')
    FROM Numeros;

    INSERT INTO @Produtos (Ordem, ProdutoId, PrecoUnitario)
    SELECT
        ROW_NUMBER() OVER (ORDER BY produto.Id),
        produto.Id,
        produto.PrecoUnitario
    FROM
    (
        SELECT TOP (2) Id, PrecoUnitario
        FROM dbo.Produtos
        ORDER BY Id
    ) AS produto;

    IF (SELECT COUNT(*) FROM @Produtos) <> 2
        THROW 51000, 'O benchmark requer pelo menos dois produtos cadastrados.', 1;

    SELECT @PrecoProduto1 = PrecoUnitario
    FROM @Produtos
    WHERE Ordem = 1;

    SELECT @PrecoProduto2 = PrecoUnitario
    FROM @Produtos
    WHERE Ordem = 2;

    INSERT INTO @Anos (Ano) VALUES (2025), (2026), (2027);

    INSERT INTO @Meses (Mes)
    VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12);

    ;WITH ClientesBenchmark AS
    (
        SELECT
            ClienteId,
            TRY_CONVERT(INT, SUBSTRING(Email, 7, CHARINDEX(N'@', Email) - 7)) AS Numero
        FROM @BenchmarkClientes
    )
    INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal)
    SELECT
        cliente.ClienteId,
        DATEFROMPARTS(ano.Ano, mes.Mes, 15),
        @PrecoProduto1 * (1 + cliente.Numero % 5) + @PrecoProduto2
    FROM ClientesBenchmark AS cliente
    CROSS JOIN @Anos AS ano
    CROSS JOIN @Meses AS mes
    WHERE cliente.Numero <= 950;

    ;WITH ClientesBenchmark AS
    (
        SELECT
            ClienteId,
            TRY_CONVERT(INT, SUBSTRING(Email, 7, CHARINDEX(N'@', Email) - 7)) AS Numero
        FROM @BenchmarkClientes
    )
    INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
    SELECT
        pedido.Id,
        produto.ProdutoId,
        CASE WHEN produto.Ordem = 1 THEN 1 + cliente.Numero % 5 ELSE 1 END,
        produto.PrecoUnitario
    FROM dbo.Pedidos AS pedido
    INNER JOIN ClientesBenchmark AS cliente ON cliente.ClienteId = pedido.ClienteId
    CROSS JOIN @Produtos AS produto
    WHERE pedido.DataPedido >= @BenchmarkStart
      AND pedido.DataPedido < @BenchmarkEnd;

    PRINT N'Massa temporária: 1.000 clientes (50 sem pedidos), 34.200 pedidos e 68.400 itens.';
    PRINT N'ANTES: query original com YEAR(DataPedido), LEFT JOIN, sem TOP e ordenação ASC implícita.';
    SET STATISTICS IO ON;
    SET STATISTICS TIME ON;
    SET STATISTICS XML ON;

    SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
    FROM dbo.Clientes AS c
    LEFT JOIN dbo.Pedidos AS p ON p.ClienteId = c.Id
    LEFT JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
    WHERE YEAR(p.DataPedido) = 2026
    GROUP BY c.Nome
    ORDER BY Total
    OPTION (RECOMPILE);

    PRINT N'DEPOIS: intervalo sargable, INNER JOIN explícito, total persistido, agrupamento por Id e TOP (5) DESC.';

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
    ORDER BY TotalComprado DESC, c.Id ASC
    OPTION (RECOMPILE);

    SET STATISTICS XML OFF;
    SET STATISTICS TIME OFF;
    SET STATISTICS IO OFF;

    ROLLBACK TRANSACTION;
    DECLARE @MaxId INT;
    DECLARE @ReseedCommand NVARCHAR(1000);

    SELECT @MaxId = MAX(Id) FROM dbo.Clientes;
    SET @ReseedCommand = N'DBCC CHECKIDENT (''dbo.Clientes'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxId) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedCommand;

    SELECT @MaxId = MAX(Id) FROM dbo.Pedidos;
    SET @ReseedCommand = N'DBCC CHECKIDENT (''dbo.Pedidos'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxId) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedCommand;

    SELECT @MaxId = MAX(Id) FROM dbo.ItensPedido;
    SET @ReseedCommand = N'DBCC CHECKIDENT (''dbo.ItensPedido'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxId) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedCommand;

    PRINT N'ROLLBACK concluído: clientes, pedidos e itens do benchmark não foram persistidos.';
END TRY
BEGIN CATCH
    SET STATISTICS XML OFF;
    SET STATISTICS TIME OFF;
    SET STATISTICS IO OFF;

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @MaxIdAfterFailure INT;
    DECLARE @ReseedAfterFailure NVARCHAR(1000);

    SELECT @MaxIdAfterFailure = MAX(Id) FROM dbo.Clientes;
    SET @ReseedAfterFailure = N'DBCC CHECKIDENT (''dbo.Clientes'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxIdAfterFailure) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedAfterFailure;

    SELECT @MaxIdAfterFailure = MAX(Id) FROM dbo.Pedidos;
    SET @ReseedAfterFailure = N'DBCC CHECKIDENT (''dbo.Pedidos'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxIdAfterFailure) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedAfterFailure;

    SELECT @MaxIdAfterFailure = MAX(Id) FROM dbo.ItensPedido;
    SET @ReseedAfterFailure = N'DBCC CHECKIDENT (''dbo.ItensPedido'', RESEED, '
        + CONVERT(NVARCHAR(20), @MaxIdAfterFailure) + N') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedAfterFailure;

    THROW;
END CATCH;
