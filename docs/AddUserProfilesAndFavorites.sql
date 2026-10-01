BEGIN TRANSACTION;
CREATE TABLE [UserProfiles] (
    [Id] int NOT NULL IDENTITY,
    [AuthProviderUserId] nvarchar(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [Role] nvarchar(32) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id])
);

CREATE TABLE [Favorites] (
    [UserProfileId] int NOT NULL,
    [ProductId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Favorites] PRIMARY KEY ([UserProfileId], [ProductId]),
    CONSTRAINT [FK_Favorites_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Favorites_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Favorites_ProductId] ON [Favorites] ([ProductId]);

CREATE UNIQUE INDEX [IX_UserProfiles_AuthProviderUserId] ON [UserProfiles] ([AuthProviderUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261001192501_AddUserProfilesAndFavorites', N'10.0.11');

COMMIT;
GO
