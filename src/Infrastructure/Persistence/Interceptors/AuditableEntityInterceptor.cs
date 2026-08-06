using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Domain.Common.Base;

namespace Sportner.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditing(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditing(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditing(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();
        var currentUserId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Property(entity => entity.CreatedAt).CurrentValue = utcNow;
                }

                if (entry.Entity.CreatedByUserId is null && currentUserId is not null)
                {
                    entry.Property(entity => entity.CreatedByUserId).CurrentValue = currentUserId;
                }
            }

            if (entry.State is EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedAt).CurrentValue = utcNow;

                if (currentUserId is not null)
                {
                    entry.Property(entity => entity.UpdatedByUserId).CurrentValue = currentUserId;
                }
            }
        }
    }
}
