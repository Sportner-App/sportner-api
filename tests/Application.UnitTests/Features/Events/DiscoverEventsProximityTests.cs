using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.DiscoverEvents;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Sports;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class DiscoverEventsProximityTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    // Kadıköy civarı referans nokta.
    private const decimal OriginLat = 40.990m;
    private const decimal OriginLng = 29.030m;

    [Fact]
    public async Task Handle_OrdersByProximity_WhenOriginIsSupplied()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());

        // En yakın olan kasten en geç tarihli: mesafe sıralaması tarihi ezmeli.
        var near = CreateEvent(
            organizer.Id, sport.Id, "Yakin", 40.995m, 29.035m, time, dayOffset: 9);
        var middle = CreateEvent(
            organizer.Id, sport.Id, "Orta", 41.060m, 29.120m, time, dayOffset: 5);
        var far = CreateEvent(
            organizer.Id, sport.Id, "Uzak", 39.930m, 32.860m, time, dayOffset: 1);

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.AddRange(far, middle, near);
        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(null), time);
        var result = await handler.Handle(
            new DiscoverEventsQuery(Latitude: OriginLat, Longitude: OriginLng),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(item => item.Title)
            .Should().ContainInOrder("Yakin", "Orta", "Uzak");
    }

    [Fact]
    public async Task Handle_OrdersByDate_WhenOriginIsMissing()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());

        var near = CreateEvent(
            organizer.Id, sport.Id, "Yakin", 40.995m, 29.035m, time, dayOffset: 9);
        var far = CreateEvent(
            organizer.Id, sport.Id, "Uzak", 39.930m, 32.860m, time, dayOffset: 1);

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.AddRange(near, far);
        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(null), time);
        var result = await handler.Handle(new DiscoverEventsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(item => item.Title)
            .Should().ContainInOrder("Uzak", "Yakin");
    }

    [Fact]
    public async Task Handle_KeepsNearestFirstAcrossPages()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        for (var index = 0; index < 6; index++)
        {
            db.Events.Add(CreateEvent(
                organizer.Id,
                sport.Id,
                $"E{index}",
                OriginLat + (index * 0.02m),
                OriginLng,
                time,
                dayOffset: 6 - index));
        }

        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(null), time);

        var first = await handler.Handle(
            new DiscoverEventsQuery(
                Latitude: OriginLat, Longitude: OriginLng, Page: 1, PageSize: 3),
            CancellationToken.None);
        var second = await handler.Handle(
            new DiscoverEventsQuery(
                Latitude: OriginLat, Longitude: OriginLng, Page: 2, PageSize: 3),
            CancellationToken.None);

        first.Value!.Items.Select(item => item.Title)
            .Should().ContainInOrder("E0", "E1", "E2");
        second.Value!.Items.Select(item => item.Title)
            .Should().ContainInOrder("E3", "E4", "E5");
    }

    /// <summary>
    /// Yakınlık sıralaması saf SQL aritmetiğine çevrilebilmeli. InMemory
    /// sağlayıcı LINQ'i bellekte çalıştırdığı için çeviriyi doğrulamaz;
    /// burada Npgsql'e karşı SQL üretiyoruz (bağlantı açılmaz).
    /// </summary>
    [Fact]
    public void ProximityOrdering_TranslatesToSql()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=sportner_translation_check")
            .Options;

        using var db = new AppDbContext(options);

        const decimal lat = OriginLat;
        const decimal lng = OriginLng;
        var lngScale = (decimal)Math.Cos((double)lat * Math.PI / 180.0);

        var joined =
            from @event in db.Events.AsNoTracking()
            join sport in db.Sports.AsNoTracking() on @event.SportId equals sport.Id
            select new { Event = @event, Sport = sport };

        var sql = joined
            .OrderBy(row =>
                (row.Event.Latitude - lat) * (row.Event.Latitude - lat)
                + (row.Event.Longitude - lng)
                    * (row.Event.Longitude - lng)
                    * lngScale
                    * lngScale)
            .ThenBy(row => row.Event.EventDate)
            .ThenBy(row => row.Event.Id)
            .Select(row => new { row.Event.Id, row.Sport.Name })
            .Skip(0)
            .Take(20)
            .ToQueryString();

        sql.Should().Contain("ORDER BY");
        // Sıralama sunucuda yapılmalı; istemciye çekilip bellekte
        // sıralanıyorsa sayfalama sessizce yanlış sayfayı döner.
        sql.Should().Contain("LIMIT");

        var orderByClause = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
        orderByClause.Should().Contain("Latitude");
        orderByClause.Should().Contain("Longitude");
    }

    private static DomainEvent CreateEvent(
        Guid organizerId,
        Guid sportId,
        string title,
        decimal latitude,
        decimal longitude,
        FakeTimeProvider time,
        int dayOffset)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            title,
            time.GetUtcNow().AddDays(dayOffset),
            90,
            latitude,
            longitude,
            "İstanbul",
            time.GetUtcNow(),
            maxParticipants: 10);
        @event.Publish(time.GetUtcNow());
        return @event;
    }
}
