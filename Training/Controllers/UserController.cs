using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Training.Dtos;
using Training.Services;

namespace Training.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsers();
        
        return Ok(users);   // Returning an empty list is not an error
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(int userId)
    {
        var user = await _userService.GetUser(userId);
        return Ok(user);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> EditUser(int userId, UserUpdateDto userDto)
    {
        await _userService.EditUser(userId, userDto);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        await _userService.DeleteUser(userId);
        return NoContent();
    }
}

