using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Training.Dtos;
using Training.Services;

namespace Training.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authServices;

    public AuthController(AuthService authServices)
    {
        _authServices = authServices;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto registerRequestDto)
    {
        await _authServices.Register(registerRequestDto);
        return Created();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto userForLoginDto)
    {
        var accessToken = await _authServices.Login(userForLoginDto);
        return Ok(accessToken);
    }

    [AllowAnonymous]
    [HttpPost("refreshtoken")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        var accessToken = await _authServices.RefreshToken(refreshTokenDto);
        return Ok(accessToken);
    }

    [AllowAnonymous]    // In case access token is expired
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenDto refreshToken)
    {
        await _authServices.Logout(refreshToken);
        return Ok();
    }
}