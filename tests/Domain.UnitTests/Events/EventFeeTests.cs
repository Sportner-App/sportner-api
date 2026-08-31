using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventFeeTests
{
    [Fact]
    public void Create_defaults_to_free()
    {
        var @event = CreateEvent();

        @event.IsPaid.Should().BeFalse();
        @event.FeeAmount.Should().BeNull();
    }

    [Fact]
    public void Create_free_clears_supplied_fee_amount()
    {
        var @event = CreateEvent(isPaid: false, feeAmount: 80m);

        @event.IsPaid.Should().BeFalse();
        @event.FeeAmount.Should().BeNull();
    }

    [Fact]
    public void Create_paid_stores_rounded_fee()
    {
        var @event = CreateEvent(isPaid: true, feeAmount: 149.994m);

        @event.IsPaid.Should().BeTrue();
        @event.FeeAmount.Should().Be(149.99m);
    }

    [Fact]
    public void Create_paid_without_fee_throws()
    {
        var act = () => CreateEvent(isPaid: true, feeAmount: null);

        act.Should().Throw<DomainException>()
            .WithMessage("Fee amount is required for paid events.");
    }

    [Fact]
    public void Create_paid_with_zero_fee_throws()
    {
        var act = () => CreateEvent(isPaid: true, feeAmount: 0m);

        act.Should().Throw<DomainException>()
            .WithMessage("Fee amount must be greater than zero.");
    }

    [Fact]
    public void UpdateFee_switches_event_to_paid()
    {
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now);

        @event.UpdateFee(true, 200m, now.AddMinutes(1));

        @event.IsPaid.Should().BeTrue();
        @event.FeeAmount.Should().Be(200m);
    }

    private static Event CreateEvent(
        DateTimeOffset? now = null,
        bool isPaid = false,
        decimal? feeAmount = null)
    {
        var utcNow = now ?? new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var sport = Sport.Create("Football", 1, utcNow, "football");

        return Event.Create(
            Guid.NewGuid(),
            sport.Id,
            "Match",
            utcNow.AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            utcNow,
            isPaid: isPaid,
            feeAmount: feeAmount);
    }
}
