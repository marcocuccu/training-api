using Microsoft.Extensions.Logging;
using Moq;
using Training.Data;
using Training.Dtos;
using Training.Entities;
using Training.Services;

namespace Training.Tests.Unit.Services;

public class UserServiceTests
{

    #region FACTORY_METHODS
    private static User CreateUser(int userId = 1, string firstName = "FirstName", string lastName = "LastName", string email = "test@email.com", string? gender = null, bool active = true)
    {
        return new User
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Gender = gender,
            Active = active
        };
    }
    private static List<User> CreateUsers(int howManyUsers)
    {
        var users = new List<User>();
        for(int i = 1; i <= howManyUsers; i++)
        {
            users.Add(CreateUser(userId: i, email: $"test{i}@email.com"));
        }
        return users;
    }
    #endregion

    [Fact]
    [Trait("Category", "GetUser")]
    public async Task GetUser_WhenUserExists_ReturnsExistingUser()
    {
        // ## ARRANGE
        var expectedUserId = 1;
        var expectedFirstName = "firstName";
        var expectedLastName = "lastName";
        var expectedEmail = "expected@email.com";

        var userForRepo = CreateUser(userId: expectedUserId, firstName: expectedFirstName, lastName: expectedLastName, email: expectedEmail);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(expectedUserId))
            .ReturnsAsync(userForRepo);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        var user = await userService.GetUser(expectedUserId);

        // ## ASSERT
        Assert.NotNull(user);
        Assert.Equal(expectedUserId, user.Id);
        Assert.Equal($"{expectedFirstName} {expectedLastName}", user.FullName);
        Assert.Equal(expectedEmail, user.Email);
        Assert.Null(user.Gender);
    }

    [Fact]
    [Trait("Category", "GetUser")]
    public async Task GetUser_WhenUserDoNotExists_ThrowsKeyNotFoundException()
    {
        // ## ARRANGE
        int missingUserId = 999;
        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(missingUserId))
            .ReturnsAsync((User?)null);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        async Task Act() => await userService.GetUser(missingUserId);

        // ## ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(Act);
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersExist_ReturnsUsers()
    {
        // ## ARRANGE
        var usersFromRepository = new List<User>
        {
            CreateUser(
                userId: 1,
                firstName: "test1",
                lastName: "test1",
                email: "test1@email.com",
                gender: "test1"
            ),
            CreateUser(
                userId: 2,
                firstName: "test2",
                lastName: "test2",
                email: "test2@email.com",
                gender: "test2"
            )
        };

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUsers())
            .ReturnsAsync(usersFromRepository);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        var result = (await userService.GetUsers()).ToList();

        // ## ASSERT
        Assert.Equal(2, result.Count);

        Assert.Contains(result, u =>
            u.Id == 1 &&
            u.FullName == "test1 test1" &&
            u.Email == "test1@email.com" &&
            u.Gender == "test1");

        Assert.Contains(result, u =>
            u.Id == 2 &&
            u.FullName == "test2 test2" &&
            u.Email == "test2@email.com" &&
            u.Gender == "test2");
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersDontExist_ReturnsEmptyCollection()
    {
        // ## ARRANGE
        var users = CreateUsers(0);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUsers())
            .ReturnsAsync(users);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        var result = await userService.GetUsers();

        // ## ASSERT
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersAreAlsoNotActive_ReturnsOnlyActive()
    {
        // ## ARRANGE
        var usersFromRepository = new List<User>
        {
            CreateUser(userId: 1, email: "test1@email.com", active: true),
            CreateUser(userId: 2, email: "test2@email.com", active: false),
            CreateUser(userId: 3, email: "test3@email.com", active: true),
            CreateUser(userId: 4, email: "test4@email.com", active: false),
        };

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUsers())
            .ReturnsAsync(usersFromRepository);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        var users = await userService.GetUsers();

        // ## ASSERT
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.Equal(2, users.Count());
        Assert.Contains(users, u => u.Email == "test1@email.com");
        Assert.Contains(users, u => u.Email == "test3@email.com");
        Assert.DoesNotContain(users, u => u.Email == "test2@email.com");
        Assert.DoesNotContain(users, u => u.Email == "test4@email.com");
    }

    [Fact]
    [Trait("Category", "EditUser")]
    public async Task EditUser_WhenUserExists_CorrectlyEditsUser()
    {
        // ## ARRANGE
        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        // Mocking GetUser
        int userIdToEdit = 1;
        var existingUser = CreateUser(userId: userIdToEdit);
        
        userRepo
            .Setup(repo => repo.GetUser(userIdToEdit))
            .ReturnsAsync(existingUser);

        // Mocking EditUser
        var userUpdateDto = new UserUpdateDto()
        {
            FirstName = "newFirstName",
            LastName = "newLastName",
            Email = "new@email.com",
            Gender = "newGender"
        };
        
        // UserService.EditUser calls repo.GetUser first to check if the user exists.
        // Then it calls repo.EditUser with a User object created from UserUpdateDto.
        // if passing a User as parameter for repo.EditUser, it would expect the exact same instance in memory.
        // Using It.Is<> instead we say to expect an object with that data
        userRepo
            .Setup(repo => repo.EditUser(
                userIdToEdit, 
                It.IsAny<User>()))
            .ReturnsAsync(true);

        // UserService creation
        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        bool success = await userService.EditUser(userIdToEdit, userUpdateDto);

        // ## ASSERT
        Assert.True(success);

        userRepo.Verify(repo => repo.EditUser(
            userIdToEdit,
            It.Is<User>(u => 
                u.FirstName == userUpdateDto.FirstName &&
                u.LastName == userUpdateDto.LastName &&
                u.Email == userUpdateDto.Email &&
                u.Gender == userUpdateDto.Gender)),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "EditUser")]
    public async Task EditUser_WhenOnlyFirstNameIsProvided_PreservesExistingValues()
    {
        // ## ARRANGE
        int userId = 1;

        var existingUser = CreateUser(
            userId: userId,
            firstName: "OldFirstName",
            lastName: "OldLastName",
            email: "old@email.com",
            gender: "OldGender"
        );

        var updateDto = new UserUpdateDto
        {
            FirstName = "NewFirstName"
        };

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(userId))
            .ReturnsAsync(existingUser);

        userRepo
            .Setup(repo => repo.EditUser(userId, It.IsAny<User>()))
            .ReturnsAsync(true);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        var success = await userService.EditUser(userId, updateDto);

        // ## ASSERT
        Assert.True(success);

        userRepo.Verify(repo => repo.EditUser(
            userId,
            It.Is<User>(u =>
                u.UserId == userId &&
                u.FirstName == "NewFirstName" &&
                u.LastName == "OldLastName" &&
                u.Email == "old@email.com" &&
                u.Gender == "OldGender"
            )),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "EditUser")]
    public async Task EditUser_WhenRepoFails_ThrowsInvalidOperationException()
    {
        // ## ARRANGE
        int userId = 1;

        var existingUser = CreateUser(userId: userId);

        var updateDto = new UserUpdateDto
        {
            FirstName = "NewFirstName"
        };

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(userId))
            .ReturnsAsync(existingUser);

        userRepo
            .Setup(repo => repo.EditUser(userId, It.IsAny<User>()))
            .ReturnsAsync(false);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        async Task Act() => await userService.EditUser(userId, updateDto);

        // ## ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    [Trait("Category", "DeleteUser")]
    public async Task DeleteUser_WhenUserExists_CorrectlyDeletesUser()
    {
        // ## ARRANGE
        int userId = 1;

        var user = CreateUser(userId: userId);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        var userService = new UserService(userRepo.Object, logger.Object);

        userRepo
            .Setup(repo => repo.GetUser(userId))
            .ReturnsAsync(user);
        
        userRepo
            .Setup(repo => repo.DeleteUser(userId))
            .ReturnsAsync(true);

        // ## ACT
        bool success = await userService.DeleteUser(userId);

        // ## ASSERT
        Assert.True(success);

        userRepo.Verify(repo => repo.DeleteUser(userId), Times.Once);
        userRepo.Verify(repo => repo.GetUser(userId), Times.Once);
    }

    [Fact]
    [Trait("Category", "DeleteUser")]
    public async Task DeleteUser_WhenUserDoNotExists_ThrowsKeyNotFoundException()
    {
        // ## ARRANGE   
        int existingUserId = 1;
        int fakeUserId = 999;

        var user = CreateUser(existingUserId);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(existingUserId))
            .ReturnsAsync(user);

        userRepo
            .Setup(repo => repo.DeleteUser(It.IsAny<int>()))
            .ReturnsAsync(true);

        var userService = new UserService(userRepo.Object, logger.Object);
        
        // ## ACT   
        async Task Act() => await userService.DeleteUser(fakeUserId);

        // ## ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(Act);
    }

    [Fact]
    [Trait("Category", "DeleteUser")]
    public async Task DeleteUser_WhenRepositoryFails_ThrowsInvalidOperationException()
    {
        // ## ARRANGE
        int userId = 1;

        var existingUser = CreateUser(userId: userId);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(userId))
            .ReturnsAsync(existingUser);

        userRepo
            .Setup(repo => repo.DeleteUser(userId))
            .ReturnsAsync(false);

        var userService = new UserService(userRepo.Object, logger.Object);

        // ## ACT
        async Task Act() => await userService.DeleteUser(userId);

        // ## ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }
}