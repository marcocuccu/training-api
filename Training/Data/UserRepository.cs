using Training.Entities;

namespace Training.Data;

public class UserRepository : IUserRepository
{
    private readonly DataContextDapper _dapper;

    public UserRepository(DataContextDapper dapper)
    {
        _dapper = dapper;
    }
    
    public async Task<IEnumerable<User>> GetUsers()
    {
        string sql = @$"
            SELECT *
            FROM MainSchema.Users
        ";

        return await _dapper.QueryAsync<User>(sql);
    }

    public async Task<User?> GetUser(int userId)
    {
        string sql = @$"
            SELECT *
            FROM MainSchema.Users
            WHERE UserId = @UserId
        ";
        
        return await _dapper.QuerySingleOrDefaultAsync<User>(sql, new
        {
            @UserId = userId
        });
    }

    public async Task<bool> EditUser(int userId, User userDto)
    {
        string sql = @$"
            UPDATE MainSchema.Users
            SET 
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                Gender = @Gender
            WHERE UserId = @UserId
        ";

        return await _dapper.ExecuteAsyncBool(sql, new
        {
            userDto.FirstName,
            userDto.LastName,
            userDto.Email,
            userDto.Gender,
            userId
        });
    }

    public async Task<bool> DeleteUser(int userId)
    {
        string sql = @$"
            DELETE MainSchema.Users
            WHERE UserId = @UserId
        ";

        return await _dapper.ExecuteAsyncBool(sql, new
        {
            userId
        });
    }
}