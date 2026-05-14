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
        // Arrange
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

        // Act
        var user = await userService.GetUser(expectedUserId);

        // Assert
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
        // Arrange
        int missingUserId = 999;
        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUser(missingUserId))
            .ReturnsAsync((User?)null);

        var userService = new UserService(userRepo.Object, logger.Object);

        // Act
        async Task Act() => await userService.GetUser(missingUserId);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(Act);
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersExist_ReturnsNotEmptyCollection()
    {
        // Arrange
        var usersFromRepository = CreateUsers(2);

        var userRepositoryMock = new Mock<IUserRepository>();
        var loggerMock = new Mock<ILogger<UserService>>();

        userRepositoryMock
            .Setup(repo => repo.GetUsers())
            .ReturnsAsync(usersFromRepository);

        var userService = new UserService(userRepositoryMock.Object, loggerMock.Object);

        // Act
        var users = await userService.GetUsers();

        // Assert
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersDontExist_ReturnsEmptyCollection()
    {
        // Arrange
        var users = CreateUsers(0);

        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        userRepo
            .Setup(repo => repo.GetUsers())
            .ReturnsAsync(users);

        var userService = new UserService(userRepo.Object, logger.Object);

        // Act
        var result = await userService.GetUsers();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "GetUsers")]
    public async Task GetUsers_WhenUsersAreAlsoNotActive_ReturnsOnlyActive()
    {
        // Arrange
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

        // Act
        var users = await userService.GetUsers();

        // Assert
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
}