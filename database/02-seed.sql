USE JoyaPedidos;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE Email = N'maria.santos@example.com')
BEGIN
    INSERT INTO dbo.Clientes (Nome, Email)
    VALUES (N'Maria Santos', N'maria.santos@example.com');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE Email = N'joao.lima@example.com')
BEGIN
    INSERT INTO dbo.Clientes (Nome, Email)
    VALUES (N'João Lima', N'joao.lima@example.com');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE Email = N'ana.costa@example.com')
BEGIN
    INSERT INTO dbo.Clientes (Nome, Email)
    VALUES (N'Ana Costa', N'ana.costa@example.com');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE Nome = N'Teclado mecânico')
BEGIN
    INSERT INTO dbo.Produtos (Nome, PrecoUnitario, EstoqueDisponivel)
    VALUES (N'Teclado mecânico', 299.90, 18);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE Nome = N'Mouse sem fio')
BEGIN
    INSERT INTO dbo.Produtos (Nome, PrecoUnitario, EstoqueDisponivel)
    VALUES (N'Mouse sem fio', 149.90, 25);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE Nome = N'Monitor 24 polegadas')
BEGIN
    INSERT INTO dbo.Produtos (Nome, PrecoUnitario, EstoqueDisponivel)
    VALUES (N'Monitor 24 polegadas', 899.00, 10);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE Nome = N'Webcam HD')
BEGIN
    INSERT INTO dbo.Produtos (Nome, PrecoUnitario, EstoqueDisponivel)
    VALUES (N'Webcam HD', 219.50, 15);
END;
GO
