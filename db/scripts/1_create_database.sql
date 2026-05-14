 -- Creates the DB if it does not exist already
 -- DB_NAME is passed as parameter from docker-compose.yml

IF DB_ID(N'$(DB_NAME)') IS NULL
BEGIN
    PRINT 'Creating DB $(DB_NAME)';
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(N'$(DB_NAME)');
    EXEC(@sql);
END
ELSE
BEGIN
    PRINT 'DB $(DB_NAME) already exists';
END
GO
