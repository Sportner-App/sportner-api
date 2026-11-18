using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Admin.Announcements;

public sealed record AnnouncementSport(Guid Id, string Name);

public sealed record AnnouncementOptions(IReadOnlyList<string> Cities, IReadOnlyList<AnnouncementSport> Sports);

public sealed record AnnouncementPreview(int RecipientCount);

public sealed record AnnouncementSendResult(int SentCount);

internal static class AdminAnnouncementRules
{
    internal const int MaxTitleLength = 65;
    internal const int MaxBodyLength = 240;
    internal const int BatchSize = 500;
    internal static readonly TimeSpan SendCooldown = TimeSpan.FromMinutes(5);

    internal static bool IsValidTitle(string? title) =>
        title?.Trim() is { Length: >= 1 and <= MaxTitleLength };

    internal static bool IsValidBody(string? body) =>
        body?.Trim() is { Length: >= 1 and <= MaxBodyLength };

    /// <summary>
    /// Önizleme ve gönderim aynı alıcı kümesini kullanır: giriş yapabilen (aktif), test hesabı olmayan
    /// ve duyuru bildirimine açıkça (push) izin vermiş kullanıcılar; isteğe bağlı şehir / spor süzgeciyle.
    /// Tercih satırı olmayan kullanıcı "izin yok" sayılır.
    /// </summary>
    internal static IQueryable<User> Recipients(IApplicationDbContext dbContext, string? city, Guid? sportId)
    {
        var settings = dbContext.NotificationSettings;
        var query = dbContext.Users.AsNoTracking()
            .Where(user => user.Status == UserStatus.Active
                && !user.IsTestAccount
                && settings.Any(setting => setting.UserId == user.Id
                    && setting.NotificationType == NotificationType.Announcement
                    && setting.PushEnabled));

        var cityVariants = CityVariants(city);
        if (cityVariants.Count > 0)
        {
            query = query.Where(user => user.UserProfile != null
                && user.UserProfile.City != null
                && cityVariants.Contains(user.UserProfile.City.Trim()));
        }

        if (sportId is { } sport)
        {
            var userSports = dbContext.UserSports;
            query = query.Where(user => userSports.Any(item => item.UserId == user.Id && item.SportId == sport));
        }

        return query;
    }

    /// <summary>
    /// Küçük/büyük harf duyarsızlığı veritabanına bırakılmaz (.NET ile Postgres "İ"/"ı" gibi harflerde
    /// farklı davranır). Girdinin yaygın yazımları C# tarafında üretilip IN ile karşılaştırılır.
    /// </summary>
    internal static List<string> CityVariants(string? city)
    {
        var trimmed = city?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return [];
        }

        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        return new[]
            {
                trimmed,
                trimmed.ToLower(turkish),
                trimmed.ToUpper(turkish),
                trimmed.ToLowerInvariant(),
                trimmed.ToUpperInvariant(),
                char.ToUpper(trimmed[0], turkish) + trimmed[1..].ToLower(turkish)
            }
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}

public sealed record GetAnnouncementOptionsQuery : IQuery<AnnouncementOptions>;

internal sealed class GetAnnouncementOptionsQueryHandler : IQueryHandler<GetAnnouncementOptionsQuery, AnnouncementOptions>
{
    private readonly IApplicationDbContext _dbContext;

    public GetAnnouncementOptionsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AnnouncementOptions>> Handle(
        GetAnnouncementOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var cities = await _dbContext.Cities.AsNoTracking()
            .Select(city => city.Name)
            .ToListAsync(cancellationToken);
        cities.Sort(StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true));

        var sports = await _dbContext.Sports.AsNoTracking()
            .Where(sport => sport.IsActive)
            .OrderBy(sport => sport.DisplayOrder)
            .ThenBy(sport => sport.Name)
            .Select(sport => new AnnouncementSport(sport.Id, sport.Name))
            .ToListAsync(cancellationToken);

        return Result<AnnouncementOptions>.Success(new AnnouncementOptions(cities, sports));
    }
}

public sealed record PreviewAnnouncementQuery(string Title, string Body, string? City, Guid? SportId)
    : IQuery<AnnouncementPreview>;

public sealed class PreviewAnnouncementQueryValidator : AbstractValidator<PreviewAnnouncementQuery>
{
    public PreviewAnnouncementQueryValidator()
    {
        RuleFor(query => query.Title).Must(AdminAnnouncementRules.IsValidTitle)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidAnnouncement);
        RuleFor(query => query.Body).Must(AdminAnnouncementRules.IsValidBody)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidAnnouncement);
    }
}

internal sealed class PreviewAnnouncementQueryHandler : IQueryHandler<PreviewAnnouncementQuery, AnnouncementPreview>
{
    private readonly IApplicationDbContext _dbContext;

    public PreviewAnnouncementQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AnnouncementPreview>> Handle(
        PreviewAnnouncementQuery request,
        CancellationToken cancellationToken)
    {
        var count = await AdminAnnouncementRules.Recipients(_dbContext, request.City, request.SportId)
            .CountAsync(cancellationToken);
        return Result<AnnouncementPreview>.Success(new AnnouncementPreview(count));
    }
}

public sealed record SendAnnouncementCommand(
    string Title,
    string Body,
    string? City,
    Guid? SportId,
    int ExpectedRecipientCount) : ICommand<AnnouncementSendResult>;

public sealed class SendAnnouncementCommandValidator : AbstractValidator<SendAnnouncementCommand>
{
    public SendAnnouncementCommandValidator()
    {
        RuleFor(command => command.Title).Must(AdminAnnouncementRules.IsValidTitle)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidAnnouncement);
        RuleFor(command => command.Body).Must(AdminAnnouncementRules.IsValidBody)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidAnnouncement);
    }
}

/// <summary>
/// Duyuruyu alıcılara 500'lük partilerle yazar. İşlem kaydı ilk partiden önce ayrı kaydedilir: gönderim
/// yarıda kalsa bile 5 dakikalık kilit devrede olur ve aynı duyuru ikinci kez gitmez.
/// </summary>
internal sealed class SendAnnouncementCommandHandler : ICommandHandler<SendAnnouncementCommand, AnnouncementSendResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public SendAnnouncementCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        INotificationPublisher publisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AnnouncementSendResult>> Handle(
        SendAnnouncementCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result<AnnouncementSendResult>.Failure(AdminErrors.NotAuthenticated);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var since = utcNow - AdminAnnouncementRules.SendCooldown;
        var tooSoon = await _dbContext.AdminAuditEntries.AsNoTracking()
            .AnyAsync(entry => entry.Action == AdminActions.AnnouncementSend && entry.CreatedAt > since, cancellationToken);
        if (tooSoon)
        {
            return Result<AnnouncementSendResult>.Failure(AdminErrors.AnnouncementTooSoon);
        }

        var recipientIds = await AdminAnnouncementRules.Recipients(_dbContext, request.City, request.SportId)
            .OrderBy(user => user.Id)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (recipientIds.Count == 0)
        {
            return Result<AnnouncementSendResult>.Failure(AdminErrors.AnnouncementNoRecipients);
        }

        if (recipientIds.Count != request.ExpectedRecipientCount)
        {
            return Result<AnnouncementSendResult>.Failure(AdminErrors.AnnouncementAudienceChanged);
        }

        var title = request.Title.Trim();
        var body = request.Body.Trim();
        var city = request.City?.Trim();
        city = string.IsNullOrEmpty(city) ? null : city;
        string? sportName = null;
        if (request.SportId is { } sportId)
        {
            sportName = await _dbContext.Sports.AsNoTracking()
                .Where(sport => sport.Id == sportId)
                .Select(sport => sport.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? sportId.ToString();
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId,
            AdminActions.AnnouncementSend,
            AdminActions.AnnouncementTarget,
            Guid.NewGuid(),
            $"{title} | şehir: {city ?? "tümü"} | spor: {sportName ?? "tümü"} | {recipientIds.Count} kişi",
            utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var batch in recipientIds.Chunk(AdminAnnouncementRules.BatchSize))
        {
            foreach (var recipientId in batch)
            {
                await _publisher.PublishAsync(
                    recipientId,
                    NotificationType.Announcement,
                    title,
                    body,
                    NotificationEntityType.System,
                    null,
                    null,
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Uygulama katmanı arayüzünde ChangeTracker yok; 500'lük partilerin izlenen kayıtları birikmesin.
            (_dbContext as DbContext)?.ChangeTracker.Clear();
        }

        return Result<AnnouncementSendResult>.Success(new AnnouncementSendResult(recipientIds.Count));
    }
}
