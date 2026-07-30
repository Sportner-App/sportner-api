using System.Net;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.DTOs.Reviews;
using Sportner.Application.Mappers;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;
using Sportner.Domain.Enums;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class ReviewService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        var reviewerId = RequireUserId();
        var approvedStatus = ParticipantStatus.Approved.ToDbValue();

        if (dto.ReviewedId == reviewerId)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Review_Self);
        }

        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == dto.EventId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        var reviewerParticipated = eventEntity.CreatedBy == reviewerId ||
            await unitOfWork.EventParticipants.AnyAsync(
                p => p.EventId == dto.EventId &&
                     p.UserId == reviewerId &&
                     p.Status == approvedStatus,
                cancellationToken);

        var reviewedParticipated = eventEntity.CreatedBy == dto.ReviewedId ||
            await unitOfWork.EventParticipants.AnyAsync(
                p => p.EventId == dto.EventId &&
                     p.UserId == dto.ReviewedId &&
                     p.Status == approvedStatus,
                cancellationToken);

        if (!reviewerParticipated || !reviewedParticipated)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Review_NotParticipants);
        }

        var alreadyReviewed = await unitOfWork.Reviews.AnyAsync(
            r => r.EventId == dto.EventId &&
                 r.ReviewerId == reviewerId &&
                 r.ReviewedId == dto.ReviewedId,
            cancellationToken);

        if (alreadyReviewed)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Review_AlreadyExists);
        }

        var reviewedProfile = await unitOfWork.Profiles.FindOneAsync(p => p.Id == dto.ReviewedId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Review_UserNotFound);

        var review = new Review
        {
            Id = Guid.NewGuid(),
            EventId = dto.EventId,
            ReviewerId = reviewerId,
            ReviewedId = dto.ReviewedId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Reviews.InsertOneAsync(review, cancellationToken);

        var previousCount = reviewedProfile.ReviewCount ?? 0;
        var previousAvg = reviewedProfile.AvgRating ?? 0m;
        var newCount = previousCount + 1;
        reviewedProfile.ReviewCount = newCount;
        reviewedProfile.AvgRating = ((previousAvg * previousCount) + dto.Rating) / newCount;
        reviewedProfile.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Profiles.UpdateOne(reviewedProfile);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        review.Reviewer = await unitOfWork.Profiles.FindByIdAsync(reviewerId, cancellationToken);

        return review.ToDto();
    }

    public async Task<IReadOnlyList<ReviewDto>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await unitOfWork.Reviews
            .AsQueryable()
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Where(r => r.ReviewedId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return reviews.Select(r => r.ToDto()).ToList();
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);
}
