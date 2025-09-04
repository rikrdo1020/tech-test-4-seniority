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
CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [ExternalId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Tasks] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ItemStatus] int NOT NULL,
    [DueDate] datetime2 NULL,
    [CreatedByUserId] uniqueidentifier NOT NULL,
    [AssignedToUserId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tasks_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tasks_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Tasks_AssignedToUserId] ON [Tasks] ([AssignedToUserId]);

CREATE INDEX [IX_Tasks_AssignedToUserId_ItemStatus] ON [Tasks] ([AssignedToUserId], [ItemStatus]);

CREATE INDEX [IX_Tasks_CreatedByUserId] ON [Tasks] ([CreatedByUserId]);

CREATE INDEX [IX_Tasks_CreatedByUserId_ItemStatus] ON [Tasks] ([CreatedByUserId], [ItemStatus]);

CREATE INDEX [IX_Tasks_DueDate] ON [Tasks] ([DueDate]);

CREATE INDEX [IX_Tasks_ItemStatus] ON [Tasks] ([ItemStatus]);

CREATE UNIQUE INDEX [IX_Users_ExternalId] ON [Users] ([ExternalId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250831230617_InitDb', N'9.0.8');

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientUserId] uniqueidentifier NOT NULL,
    [RelatedTaskId] uniqueidentifier NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(2000) NULL,
    [Type] int NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Tasks_RelatedTaskId] FOREIGN KEY ([RelatedTaskId]) REFERENCES [Tasks] ([Id]),
    CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Notifications_RecipientUserId] ON [Notifications] ([RecipientUserId]);

CREATE INDEX [IX_Notifications_RelatedTaskId] ON [Notifications] ([RelatedTaskId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250903070312_AddNotifications', N'9.0.8');


                CREATE PROCEDURE dbo.sp_AddNotification
                    @RecipientUserId UNIQUEIDENTIFIER,
                    @RelatedTaskId UNIQUEIDENTIFIER = NULL,
                    @Title NVARCHAR(200),
                    @Message NVARCHAR(2000) = NULL,
                    @Type INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO dbo.Notifications (RecipientUserId, RelatedTaskId, Title, Message, Type, IsRead, CreatedAt)
                    OUTPUT inserted.Id, inserted.RecipientUserId, inserted.RelatedTaskId, inserted.Title, inserted.Message, inserted.Type, inserted.IsRead, inserted.CreatedAt, inserted.ReadAt
                    VALUES (@RecipientUserId, @RelatedTaskId, @Title, @Message, @Type, 0, SYSUTCDATETIME());
                END
                

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250903072515_AddNotificationSp', N'9.0.8');


                ALTER TABLE dbo.Notifications
                ADD CONSTRAINT DF_Notifications_Id DEFAULT (NEWID()) FOR Id;
            


                CREATE OR ALTER PROCEDURE dbo.sp_AddNotification
                    @RecipientUserId UNIQUEIDENTIFIER,
                    @RelatedTaskId UNIQUEIDENTIFIER = NULL,
                    @Title NVARCHAR(200),
                    @Message NVARCHAR(2000) = NULL,
                    @Type INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

                    INSERT INTO dbo.Notifications (Id, RecipientUserId, RelatedTaskId, Title, Message, Type, IsRead, CreatedAt)
                    OUTPUT inserted.Id, inserted.RecipientUserId, inserted.RelatedTaskId, inserted.Title, inserted.Message, inserted.Type, inserted.IsRead, inserted.CreatedAt, inserted.ReadAt
                    VALUES (@NewId, @RecipientUserId, @RelatedTaskId, @Title, @Message, @Type, 0, SYSUTCDATETIME());
                END
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250903084213_AddNotificationIdDefaultAndFixSp', N'9.0.8');

COMMIT;
GO

