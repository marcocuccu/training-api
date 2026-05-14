CREATE OR ALTER PROCEDURE MainSchema.CleanExpiredTokens
/*EXEC MainSchema.CleanExpiredTokens*/
AS
BEGIN
    SET NOCOUNT ON; -- Unreliable for complex queries, better to use @@ROWCOUNT AS DeletedCount

    DELETE FROM MainSchema.RefreshTokens
    WHERE ExpiresAt < SYSUTCDATETIME()
        OR RevokedAt < DATEADD(DAY, -7, SYSUTCDATETIME());

    SELECT @@ROWCOUNT AS DeletedCount;
END