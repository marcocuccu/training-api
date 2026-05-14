using Training.Entities;

namespace Training.Data;

public interface IUserRepository
{
    public Task<IEnumerable<User>> GetUsers();
    public Task<User?> GetUser(int userId);
    public Task<bool> EditUser(int userId, User userDto);
    public Task<bool> DeleteUser(int userId);

}