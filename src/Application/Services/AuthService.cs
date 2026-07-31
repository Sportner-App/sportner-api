using System.Net;
using Sportner.Application.Abstractions;
using Sportner.Application.DTOs.Auth;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class AuthService(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var existing = await unitOfWork.Users.FindOneAsync(p => p.Email == email, cancellationToken);

        // Existing Supabase user without API password: allow setting password once.
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.PasswordHash))
            {
                throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Auth_EmailAlreadyRegistered);
            }

            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                existing.FullName = dto.FullName.Trim();
            }

            unitOfWork.Users.UpdateOne(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var upgradedToken = tokenService.CreateToken(existing);
            return new AuthResponseDto(
                upgradedToken,
                existing.Id,
                existing.Email ?? email,
                existing.FullName
            );
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim()
        };

        await unitOfWork.Users.InsertOneAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateToken(user);

        return new AuthResponseDto(
            token,
            user.Id,
            email,
            user.FullName
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await unitOfWork.Users.FindOneAsync(p => p.Email == email, cancellationToken);

        if (user is null)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Auth_InvalidCredentials);
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Auth_PasswordNotSet);
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Auth_InvalidCredentials);
        }

        var token = tokenService.CreateToken(user);

        return new AuthResponseDto(
            token,
            user.Id,
            user.Email ?? email,
            user.FullName
        );
    }

    public async Task<MessageResponseDto> UpdatePushTokenAsync(
        UpdatePushTokenDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);

        if (string.IsNullOrWhiteSpace(dto.PushToken))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Validation_PushToken_Required);
        }

        var user = await unitOfWork.Users.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Auth_UserNotFound);

        user.PushToken = dto.PushToken.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponseDto(ValidationResource.Exception_Auth_PushTokenUpdated);
    }
}
