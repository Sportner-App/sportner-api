using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;
using SportnerApi.Models;
using SportnerApi.Services;

namespace SportnerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfilesController(AppDbContext db, IStorageService storageService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId.Value, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profil bulunamadı." });
        }

        return Ok(MapToDto(profile));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId.Value, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profil bulunamadı." });
        }

        if (dto.FullName is not null)
        {
            profile.FullName = dto.FullName.Trim();
        }

        if (dto.AvatarUrl is not null)
        {
            profile.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                ? null
                : dto.AvatarUrl.Trim();
        }

        if (dto.Bio is not null)
        {
            profile.Bio = string.IsNullOrWhiteSpace(dto.Bio)
                ? null
                : dto.Bio.Trim();
        }

        if (dto.Sports is not null)
        {
            profile.Sports = dto.Sports;
        }

        if (dto.IntroVideoUrl is not null)
        {
            profile.IntroVideoUrl = string.IsNullOrWhiteSpace(dto.IntroVideoUrl)
                ? null
                : dto.IntroVideoUrl.Trim();
        }

        if (dto.BirthDate is not null)
        {
            profile.BirthDate = ToUtc(dto.BirthDate.Value);
        }

        if (dto.IsOnboarded is not null)
        {
            profile.IsOnboarded = dto.IsOnboarded.Value;
        }

        if (dto.SkillLevels is not null)
        {
            profile.SkillLevels = ToJsonbString(dto.SkillLevels.Value);
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(profile));
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes)]
    [ProducesResponseType(typeof(AvatarUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var contentTypeHeader = Request.ContentType ?? string.Empty;
        if (!contentTypeHeader.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                new
                {
                    message = "Content-Type multipart/form-data olmalı. application/json ile dosya yüklenemez. FormData kullanın ve Content-Type header'ını elle set etmeyin."
                });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Avatar dosyası gerekli. Form field adı 'file' olmalı." });
        }

        if (file.Length > MaxAvatarBytes)
        {
            return BadRequest(new { message = "Avatar en fazla 5 MB olabilir." });
        }

        var contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return BadRequest(new { message = "Sadece JPEG, PNG veya WebP yükleyebilirsiniz." });
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };

        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId.Value, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profil bulunamadı." });
        }

        await using var stream = file.OpenReadStream();
        string avatarUrl;
        try
        {
            avatarUrl = await storageService.UploadAvatarAsync(
                userId.Value,
                stream,
                contentType,
                extension,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        profile.AvatarUrl = avatarUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new AvatarUploadResponseDto(avatarUrl));
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfileById(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profil bulunamadı." });
        }

        return Ok(MapToDto(profile));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Npgsql only accepts UTC DateTime for 'timestamp with time zone' columns.
    /// Date-only input such as "1999-03-28" arrives as Unspecified and is treated as UTC.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>
    /// Accepts skill levels either as a JSON object ({"football":"advanced"})
    /// or as a JSON-encoded string, and normalizes it for the jsonb column.
    /// </summary>
    private static string? ToJsonbString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()) ? null : element.GetString(),
        _ => element.GetRawText()
    };

    private static JsonElement? ParseJsonbString(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static UserProfileDto MapToDto(Profile profile) => new(
        profile.Id,
        profile.Email ?? string.Empty,
        profile.FullName,
        profile.AvatarUrl,
        profile.Bio,
        profile.Sports,
        profile.IntroVideoUrl,
        profile.IsOnboarded,
        profile.BirthDate,
        ParseJsonbString(profile.SkillLevels),
        profile.AvgRating,
        profile.ReviewCount,
        profile.PushToken,
        profile.UpdatedAt
    );
}
