using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAndOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Name] nvarchar(120) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [GlassId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CartItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [GlassId] INT NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Quantity] INT NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id])
    );
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId' AND object_id = OBJECT_ID(N'[dbo].[Orders]')
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'[dbo].[OrderItems]')
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_GlassId' AND object_id = OBJECT_ID(N'[dbo].[OrderItems]')
)
BEGIN
    CREATE INDEX [IX_OrderItems_GlassId] ON [dbo].[OrderItems]([GlassId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_GlassId' AND object_id = OBJECT_ID(N'[dbo].[CartItems]')
)
BEGIN
    CREATE INDEX [IX_CartItems_GlassId] ON [dbo].[CartItems]([GlassId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_UserId_GlassId' AND object_id = OBJECT_ID(N'[dbo].[CartItems]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartItems_UserId_GlassId] ON [dbo].[CartItems]([UserId], [GlassId]) WHERE [UserId] IS NOT NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_AspNetUsers_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [FK_Orders_AspNetUsers_UserId]
        FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderItems_Orders_OrderId'
)
BEGIN
    ALTER TABLE [dbo].[OrderItems]
    ADD CONSTRAINT [FK_OrderItems_Orders_OrderId]
        FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderItems_Glasses_GlassId'
)
BEGIN
    ALTER TABLE [dbo].[OrderItems]
    ADD CONSTRAINT [FK_OrderItems_Glasses_GlassId]
        FOREIGN KEY([GlassId]) REFERENCES [dbo].[Glasses]([Id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartItems_Glasses_GlassId'
)
BEGIN
    ALTER TABLE [dbo].[CartItems]
    ADD CONSTRAINT [FK_CartItems_Glasses_GlassId]
        FOREIGN KEY([GlassId]) REFERENCES [dbo].[Glasses]([Id]) ON DELETE CASCADE;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartItems_AspNetUsers_UserId'
)
BEGIN
    ALTER TABLE [dbo].[CartItems]
    ADD CONSTRAINT [FK_CartItems_AspNetUsers_UserId]
        FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NOT NULL DROP TABLE [dbo].[CartItems];
IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NOT NULL DROP TABLE [dbo].[OrderItems];
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL DROP TABLE [dbo].[Orders];
");
        }
    }
}
