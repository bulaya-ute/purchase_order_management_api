using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Auth;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _authService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(AppClaimTypes.CompanyId, user.CompanyId.ToString()),
        };

        claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(ToDto(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult<CurrentUserDto> Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var companyIdValue = User.FindFirstValue(AppClaimTypes.CompanyId);

        var dto = new CurrentUserDto
        {
            Id = userId,
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            CompanyId = int.TryParse(companyIdValue, out var companyId) ? companyId : 0,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        };

        return Ok(dto);
    }

    private static CurrentUserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        CompanyId = user.CompanyId,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray(),
    };
}
