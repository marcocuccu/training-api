using Training.Data;
using Training.Dtos;
using Training.Entities;

namespace Training.Services;

public class UserService
{
    private readonly IUserRepository _userRepo;
    private readonly ILogger<UserService> _logger; 
    public UserService(IUserRepository userRepo, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<UserReadDto>> GetUsers()
    {
        var usersFromDB = await _userRepo.GetUsers();

        var users = usersFromDB
            .Where(u => u.Active)
            .Select(u => new UserReadDto
            {
                Id = u.UserId,
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email,
                Gender = u.Gender
            });
        
        return users;
    }

    public async Task<UserReadDto> GetUser(int userId)
    {
        var userFromDB = await _userRepo.GetUser(userId);

        if (userFromDB is null)
        {
            throw new KeyNotFoundException($"User {userId} not found");
        }

        return new UserReadDto
        {
            Id = userFromDB.UserId,
            FullName = $"{userFromDB.FirstName} {userFromDB.LastName}",
            Email = userFromDB.Email,
            Gender = userFromDB.Gender
        };
    }

    public async Task<bool> EditUser(int userId, UserUpdateDto userDto)
    {
        var userDB = await _userRepo.GetUser(userId);

        if (userDB is null)
            throw new KeyNotFoundException($"User {userId} not found");
        
        var user = new User
        {
            UserId = userId,
            FirstName = userDto.FirstName ?? userDB.FirstName,
            LastName = userDto.LastName ?? userDB.LastName,
            Email = userDto.Email ?? userDB.Email,
            Gender = userDto.Gender ?? userDB.Gender
        };

        var success = await _userRepo.EditUser(userId, user);
        if(!success)
            throw new InvalidOperationException($"Failed to edit user {userId}");

        _logger.LogInformation("User {UserId} edited successfully", userId);

        return success;
    }

    public async Task<bool> DeleteUser(int userId)
    {
        var userDB = await _userRepo.GetUser(userId);

        if (userDB is null)
            throw new KeyNotFoundException($"User {userId} not found");

        var success = await _userRepo.DeleteUser(userId);
        if (!success)
            throw new InvalidOperationException($"Failed to delete user {userId}");

        _logger.LogInformation("User {UserId} deleted successfully", userId);

        return success;
    }
}