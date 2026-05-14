-- Creates schema, tables, indexes and stored procedures

USE [$(DB_NAME)];
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'MainSchema')
BEGIN
    EXEC(N'CREATE SCHEMA MainSchema');
END
GO

IF OBJECT_ID(N'MainSchema.Users', N'U') IS NULL
BEGIN
    CREATE TABLE MainSchema.Users
    (
        UserId INT IDENTITY(1, 1),
        FirstName NVARCHAR(50),
        LastName NVARCHAR(50),
        Email NVARCHAR(50),
        Gender NVARCHAR(50),
        Active BIT,

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID(N'MainSchema.Auth', N'U') IS NULL
BEGIN
    CREATE TABLE MainSchema.Auth
    (
        AuthId INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        PasswordHash VARBINARY(MAX) NOT NULL,
        PasswordSalt VARBINARY(MAX) NOT NULL,

        CONSTRAINT PK_Auth PRIMARY KEY CLUSTERED (AuthId),
        CONSTRAINT UQ_Auth_UserId UNIQUE (UserId),
        CONSTRAINT FK_Auth_Users_UserId
            FOREIGN KEY (UserId)
            REFERENCES MainSchema.Users(UserId)
            ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'MainSchema.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE MainSchema.RefreshTokens
    (
        RefreshTokenId INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        TokenHash NVARCHAR(512) NOT NULL,
        ExpiresAt DATETIME2(7) NOT NULL,
        RevokedAt DATETIME2(7) NULL,
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (RefreshTokenId),
        CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshTokens_Users_UserId
            FOREIGN KEY (UserId)
            REFERENCES MainSchema.Users(UserId)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_RefreshTokens_UserId'
      AND object_id = OBJECT_ID(N'MainSchema.RefreshTokens')
)
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId
        ON MainSchema.RefreshTokens(UserId);
END
GO

CREATE OR ALTER PROCEDURE MainSchema.CleanExpiredTokens
AS
BEGIN
    SET NOCOUNT ON;     -- Unreliable for complex queries, better to use @@ROWCOUNT AS DeletedCount

    DELETE FROM MainSchema.RefreshTokens
    WHERE ExpiresAt < SYSUTCDATETIME()
       OR RevokedAt < DATEADD(DAY, -7, SYSUTCDATETIME());

    SELECT @@ROWCOUNT AS DeletedCount;
END
GO
