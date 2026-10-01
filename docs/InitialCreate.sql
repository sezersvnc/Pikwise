IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Brands] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Stock] int NOT NULL,
    [IsActive] bit NOT NULL,
    [BrandId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
);

CREATE TABLE [LaptopSpecifications] (
    [Id] int NOT NULL IDENTITY,
    [Processor] nvarchar(200) NOT NULL,
    [GPU] nvarchar(200) NOT NULL,
    [RamGb] int NOT NULL,
    [StorageGb] int NOT NULL,
    [ScreenSize] decimal(5,2) NOT NULL,
    [Resolution] nvarchar(50) NOT NULL,
    [RefreshRate] int NOT NULL,
    [Weight] decimal(6,3) NOT NULL,
    [OperatingSystem] nvarchar(100) NOT NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_LaptopSpecifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LaptopSpecifications_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Brands_Name] ON [Brands] ([Name]);

CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);

CREATE UNIQUE INDEX [IX_LaptopSpecifications_ProductId] ON [LaptopSpecifications] ([ProductId]);

CREATE INDEX [IX_Products_BrandId] ON [Products] ([BrandId]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261001173029_InitialCreate', N'10.0.11');

COMMIT;
GO
