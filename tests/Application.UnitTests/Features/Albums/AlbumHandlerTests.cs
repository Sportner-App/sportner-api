using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Albums.AddAlbumMedia;
using Sportner.Application.Features.Albums.CreateEventAlbum;
using Sportner.Application.Features.Albums.CreateProfileAlbum;
using Sportner.Application.Features.Albums.GetAlbumById;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Albums;

public sealed class AlbumHandlerTests
{
    [Fact]
    public async Task ProfileAlbum_CreateAndHideFromStranger_WhenPrivate()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var owner = CreateUser(db, now);
        var stranger = CreateUser(db, now);
        await db.SaveChangesAsync();

        var create = new CreateProfileAlbumCommandHandler(db, new TestCurrentUser(owner.Id), time);
        var created = await create.Handle(
            new CreateProfileAlbumCommand("Holiday", null, (short)AlbumVisibility.Private),
            CancellationToken.None);

        created.IsSuccess.Should().BeTrue();

        var get = new GetAlbumByIdQueryHandler(db, new TestCurrentUser(stranger.Id));
        var viewed = await get.Handle(new GetAlbumByIdQuery(created.Value!.Id), CancellationToken.None);
        viewed.IsSuccess.Should().BeFalse();
        viewed.Errors.Should().Contain(error => error.Code == "Album.Forbidden");
    }

    [Fact]
    public async Task EventAlbum_ApprovedParticipant_CanUpload_PendingCannot()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = CreateUser(db, now);
        var approved = CreateUser(db, now);
        var pending = CreateUser(db, now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");
        db.Sports.Add(sport);

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Match",
            now.AddDays(1),
            90,
            41m,
            29m,
            "Istanbul",
            now,
            maxParticipants: 10);
        @event.Publish(now);
        db.Events.Add(@event);

        // Organizer already added as participant by Create; add approved + pending
        @event.Apply(approved.Id, now);
        @event.ApproveParticipant(approved.Id, now);
        @event.Apply(pending.Id, now);

        await db.SaveChangesAsync();

        var createAlbum = new CreateEventAlbumCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time);
        var albumResult = await createAlbum.Handle(
            new CreateEventAlbumCommand(@event.Id, "After party", null),
            CancellationToken.None);
        albumResult.IsSuccess.Should().BeTrue();
        albumResult.Value!.Visibility.Should().Be((short)AlbumVisibility.EventParticipants);

        db.ChangeTracker.Clear();

        var storage = new Mock<IFileStorage>();
        storage
            .Setup(x => x.UploadAsync(
                StorageBuckets.Albums,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("albums/shot.jpg");

        var addApproved = new AddAlbumMediaCommandHandler(
            db,
            new TestCurrentUser(approved.Id),
            time,
            storage.Object);

        await using var image = new MemoryStream(new byte[] { 1, 2, 3 });
        var ok = await addApproved.Handle(
            new AddAlbumMediaCommand(
                albumResult.Value.Id,
                image,
                "image/jpeg",
                "shot.jpg",
                3),
            CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.MediaCount.Should().Be(1);

        var addPending = new AddAlbumMediaCommandHandler(
            db,
            new TestCurrentUser(pending.Id),
            time,
            storage.Object);
        await using var image2 = new MemoryStream(new byte[] { 4, 5, 6 });
        var denied = await addPending.Handle(
            new AddAlbumMediaCommand(
                albumResult.Value.Id,
                image2,
                "image/jpeg",
                "nope.jpg",
                3),
            CancellationToken.None);
        denied.IsSuccess.Should().BeFalse();
        denied.Errors.Should().Contain(error => error.Code == "Album.CannotUpload");
    }

    private static User CreateUser(AppDbContext db, DateTimeOffset utcNow)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        db.Users.Add(user);
        return user;
    }
}
