-- SistemaFacturacion - Instalacion inicial para desarrollo
USE [master];
GO
IF DB_ID(N'TiendaFacturacionDB') IS NULL
BEGIN
    CREATE DATABASE [TiendaFacturacionDB];
END;
GO
USE [TiendaFacturacionDB];
GO

IF OBJECT_ID(N'dbo.Categorias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categorias (
        ID_Categoria INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre VARCHAR(100) NOT NULL UNIQUE
    );
END;
GO
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        ID_Cliente INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre VARCHAR(100) NOT NULL,
        Telefono VARCHAR(20) NULL,
        Correo VARCHAR(100) NULL,
        Activo BIT NOT NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Productos (
        ID_Producto INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Codigo VARCHAR(20) NOT NULL UNIQUE,
        Nombre VARCHAR(100) NOT NULL,
        Precio DECIMAL(10,2) NOT NULL,
        Stock INT NOT NULL,
        Activo BIT NOT NULL,
        ID_Categoria INT NULL,
        CONSTRAINT FK_Productos_Categorias FOREIGN KEY (ID_Categoria)
            REFERENCES dbo.Categorias(ID_Categoria)
    );
END;
GO
IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ventas (
        ID_Venta INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Fecha DATETIME NOT NULL,
        Total DECIMAL(10,2) NOT NULL,
        Subtotal DECIMAL(10,2) NOT NULL,
        Impuesto DECIMAL(10,2) NOT NULL,
        Descuento DECIMAL(10,2) NOT NULL,
        ID_Cliente INT NULL,
        CONSTRAINT FK_Ventas_Clientes FOREIGN KEY (ID_Cliente)
            REFERENCES dbo.Clientes(ID_Cliente)
    );
END;
GO
IF OBJECT_ID(N'dbo.DetalleVentas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleVentas (
        ID_Detalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ID_Venta INT NOT NULL,
        ID_Producto INT NOT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(10,2) NOT NULL,
        Subtotal DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_Detalle_Venta FOREIGN KEY (ID_Venta)
            REFERENCES dbo.Ventas(ID_Venta),
        CONSTRAINT FK_Detalle_Producto FOREIGN KEY (ID_Producto)
            REFERENCES dbo.Productos(ID_Producto)
    );
END;
GO

-- Categorias iniciales.
INSERT INTO dbo.Categorias (Nombre)
SELECT v.Nombre
FROM (VALUES ('Bebidas'), ('Alimentos'), ('Lácteos'), ('Panadería'), ('Otros')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categorias c WHERE c.Nombre = v.Nombre);
GO

INSERT INTO dbo.Productos (Codigo, Nombre, Precio, Stock, Activo, ID_Categoria)
SELECT v.Codigo, v.Nombre, v.Precio, v.Stock, 1, c.ID_Categoria
FROM (VALUES
    ('P001', 'Coca Cola 600ml', CAST(1.25 AS DECIMAL(10,2)), 50, 'Bebidas'),
    ('P002', 'Galletas', CAST(0.75 AS DECIMAL(10,2)), 40, 'Alimentos'),
    ('P003', 'Arroz 1 libra', CAST(1.10 AS DECIMAL(10,2)), 30, 'Alimentos'),
    ('P004', 'Leche', CAST(1.50 AS DECIMAL(10,2)), 25, 'Lácteos'),
    ('P005', 'Pan dulce', CAST(0.50 AS DECIMAL(10,2)), 60, 'Panadería')
) AS v(Codigo, Nombre, Precio, Stock, Categoria)
INNER JOIN dbo.Categorias c ON c.Nombre = v.Categoria
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p WHERE p.Codigo = v.Codigo);
GO

-- Comprobacion final
SELECT ID_Categoria, Nombre FROM dbo.Categorias ORDER BY ID_Categoria;
SELECT p.Codigo, p.Nombre, p.Precio, p.Stock, p.Activo, c.Nombre AS Categoria
FROM dbo.Productos p
LEFT JOIN dbo.Categorias c ON c.ID_Categoria = p.ID_Categoria
ORDER BY p.Codigo;
GO
