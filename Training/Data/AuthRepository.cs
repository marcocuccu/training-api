using Dapper;
using Training.Entities;

namespace Training.Data;

public class AuthRepository : IAuthRepository
{
    private readonly DataContextDapper _dapper;
    public AuthRepository(DataContextDapper dapper)
    {
        _dapper = dapper;
    }

    public async Task<AuthCredentials?> GetAuthCredentialsByEmail(string email)
    {
        string sql = @$"
            SELECT
                u.UserId,
                a.PasswordHash,
                a.PasswordSalt
            FROM MainSchema.Users u
            JOIN MainSchema.Auth a ON a.UserId = u.UserId
            WHERE Email = @Email
        ";

        return await _dapper.QuerySingleOrDefaultAsync<AuthCredentials>(sql, new
        {
            @Email = email
        });
    }

    public async Task<bool> EmailAlreadyRegistered(string email)
    {
        // check the email is not registered yet
        string sql = @$"
            SELECT Email
            FROM MainSchema.Users
            WHERE Email = @Email
        ";

        var users = await _dapper.QueryAsync<string>(sql, new
        {
            @Email = email
        });

        return users.Any();
    }

    public async Task<bool> SaveRefreshToken(RefreshTokens refreshToken)
    {
        string sql = @$"
            INSERT INTO MainSchema.RefreshTokens(
                UserId,
                TokenHash,
                ExpiresAt,
                RevokedAt,
                CreatedAt
            ) VALUES (
                @UserId,
                @TokenHash,
                @ExpiresAt,
                @RevokedAt,
                @CreatedAt
            )";
        
        return await _dapper.ExecuteAsyncBool(sql, new
        {
            @UserId = refreshToken.UserId,
            @TokenHash = refreshToken.TokenHash,
            @ExpiresAt = refreshToken.ExpiresAt,
            @RevokedAt = refreshToken.RevokedAt,
            @CreatedAt = refreshToken.CreatedAt
        });
    }

    public async Task<RefreshTokens?> GetRefreshTokenModel(string tokenHash)
    {
        string sql = @$"
            SELECT *
            FROM MainSchema.RefreshTokens
            WHERE TokenHash = @TokenHash
        ";

        return await _dapper.QuerySingleOrDefaultAsync<RefreshTokens>(sql, new
        {
            @TokenHash = tokenHash
        });
    }

    public async Task<int> RegisterUserAndPassword(byte[] passwordSalt, byte[] passwordHash, User user)
    {
        using var connection = _dapper.CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            string sqlAddUser = @$"
                INSERT INTO MainSchema.Users(
                    FirstName,
                    LastName,
                    Email,
                    Gender,
                    Active
                ) VALUES (
                    @FirstName,
                    @LastName,
                    @Email,
                    @Gender,
                    @Active
                );
                
                SELECT CAST(SCOPE_IDENTITY() as int);";
            var sqlAddUserParam = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Gender,
                @Active = true
            };

            int userId = await connection.QuerySingleAsync<int>(sqlAddUser, sqlAddUserParam, transaction);

            string sqlPass = @$"
                INSERT INTO MainSchema.Auth(
                    UserId,
                    PasswordHash,
                    PasswordSalt
                ) VALUES (
                    @UserId,
                    @PasswordHash,
                    @PasswordSalt
                )
            ";
            var sqlPassParam = new
            {
                @UserId = userId,
                @PasswordHash = passwordHash,
                @PasswordSalt = passwordSalt
            };

            await connection.ExecuteAsync(sqlPass, sqlPassParam, transaction);
            

            transaction.Commit();
            return userId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RevokeTokenRefresh(string tokenHash)
    {
        string sql = @$"
            UPDATE MainSchema.RefreshTokens
            SET RevokedAt = SYSUTCDATETIME()
            WHERE TokenHash = @TokenHash
        ";

        return await _dapper.ExecuteAsyncBool(sql, new
        {
            @TokenHash = tokenHash
        });
    }

    public async Task<int> CleanExpiredTokens()
    {
        using var connection = _dapper.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<int>(
            "MainSchema.CleanExpiredTokens",
            commandType: System.Data.CommandType.StoredProcedure
        );
    }
}