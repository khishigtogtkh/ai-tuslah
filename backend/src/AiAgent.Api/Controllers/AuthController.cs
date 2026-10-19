using System.ComponentModel.DataAnnotations;
using AiAgent.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiAgent.Api.Controllers;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(1024)] string Password);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Problem(statusCode: 401, title: "Имэйл эсвэл нууц үг буруу байна.");
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || !await users.IsInRoleAsync(user, PlatformRoles.Admin))
            return Problem(statusCode: 401, title: "Имэйл эсвэл нууц үг буруу байна.");
        await signIn.SignInAsync(user, new AuthenticationProperties
        {
            IsPersistent = false, AllowRefresh = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
        return Ok(new { id = user.Id, email = user.Email });
    }

    [HttpGet("session")]
    [Authorize(Policy = PlatformRoles.Admin)]
    public async Task<IActionResult> Session()
    {
        var user = await users.GetUserAsync(User);
        if (user is null || !await users.IsInRoleAsync(user, PlatformRoles.Admin))
        {
            await signIn.SignOutAsync();
            return Problem(statusCode: 401);
        }
        return Ok(new { id = user.Id, email = user.Email });
    }
}
