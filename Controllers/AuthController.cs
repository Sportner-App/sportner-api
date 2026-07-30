using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;
using SportnerApi.Models;
using SportnerApi.Services;

namespace SportnerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var existing = await db.Profiles
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

        // Existing Supabase profile without API password: allow setting password once.
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.PasswordHash))
            {
                return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });
            }

            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                existing.FullName = dto.FullName.Trim();
            }

            await db.SaveChangesAsync(cancellationToken);

            var upgradedToken = tokenService.CreateToken(existing);
            return Ok(new AuthResponseDto(
                upgradedToken,
                existing.Id,
                existing.Email ?? email,
                existing.FullName
            ));
        }

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim()
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateToken(profile);

        return Ok(new AuthResponseDto(
            token,
            profile.Id,
            email,
            profile.FullName
        ));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await db.Profiles
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

        if (user is null)
        {
            return BadRequest(new { message = "E-posta veya şifre hatalı" });
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest(new
            {
                message = "Bu hesap için API şifresi henüz ayarlanmamış. Önce /api/auth/register ile aynı e-posta ve şifreyi gönderin."
            });
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return BadRequest(new { message = "E-posta veya şifre hatalı" });
        }

        var token = tokenService.CreateToken(user);

        return Ok(new AuthResponseDto(
            token,
            user.Id,
            user.Email ?? email,
            user.FullName
        ));
    }

    [Authorize]
    [HttpPost("update-push-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePushToken(
        [FromBody] UpdatePushTokenDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        profile.PushToken = dto.PushToken.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Push token güncellendi." });
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
