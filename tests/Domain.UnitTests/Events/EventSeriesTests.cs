using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventSeriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_is_not_part_of_a_series()
    {
        var @event = CreateEvent();

        @event.IsSeriesOccurrence.Should().BeFalse();
        @event.HasRemainingSeriesOccurrences.Should().BeFalse();
    }

    [Fact]
    public void StartSeries_stamps_the_first_occurrence()
    {
        var @event = CreateEvent();

        @event.StartSeries(intervalWeeks: 1, totalOccurrences: 3, Now);

        @event.SeriesId.Should().NotBeNull().And.NotBe(Guid.Empty);
        @event.SeriesSequence.Should().Be(1);
        @event.SeriesIntervalWeeks.Should().Be(1);
        @event.SeriesTotalOccurrences.Should().Be(3);
        @event.HasRemainingSeriesOccurrences.Should().BeTrue();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    public void StartSeries_rejects_unsupported_interval(int intervalWeeks)
    {
        var @event = CreateEvent();

        var act = () => @event.StartSeries(intervalWeeks, totalOccurrences: 3, Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(13)]
    public void StartSeries_rejects_occurrence_count_outside_bounds(int totalOccurrences)
    {
        var @event = CreateEvent();

        var act = () => @event.StartSeries(intervalWeeks: 1, totalOccurrences, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartSeries_twice_throws()
    {
        var @event = CreateEvent();
        @event.StartSeries(intervalWeeks: 1, totalOccurrences: 3, Now);

        var act = () => @event.StartSeries(intervalWeeks: 2, totalOccurrences: 4, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateNextSeriesOccurrence_clones_template_one_interval_later()
    {
        var first = CreateEvent(isPaid: true, feeAmount: 120m);
        first.StartSeries(intervalWeeks: 2, totalOccurrences: 3, Now);
        first.Publish(Now);

        var afterFirstEnded = first.EventDate.AddMinutes(first.DurationMinutes).AddMinutes(1);
        var second = first.CreateNextSeriesOccurrence(afterFirstEnded);

        second.EventDate.Should().Be(first.EventDate.AddDays(14));
        second.SeriesId.Should().Be(first.SeriesId);
        second.SeriesSequence.Should().Be(2);
        second.SeriesIntervalWeeks.Should().Be(2);
        second.SeriesTotalOccurrences.Should().Be(3);
        second.Status.Should().Be(EventStatus.Published);
        second.Title.Should().Be(first.Title);
        second.DurationMinutes.Should().Be(first.DurationMinutes);
        second.Address.Should().Be(first.Address);
        second.IsPaid.Should().BeTrue();
        second.FeeAmount.Should().Be(120m);
        second.OrganizerUserId.Should().Be(first.OrganizerUserId);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void CreateNextSeriesOccurrence_stops_after_the_final_occurrence()
    {
        var occurrence = CreateEvent();
        occurrence.StartSeries(intervalWeeks: 1, totalOccurrences: 3, Now);
        occurrence.Publish(Now);

        var clock = Now;
        for (var expected = 2; expected <= 3; expected++)
        {
            clock = occurrence.EventDate.AddMinutes(occurrence.DurationMinutes).AddMinutes(1);
            occurrence.HasRemainingSeriesOccurrences.Should().BeTrue();
            occurrence = occurrence.CreateNextSeriesOccurrence(clock);
            occurrence.SeriesSequence.Should().Be(expected);
        }

        occurrence.HasRemainingSeriesOccurrences.Should().BeFalse();

        var act = () => occurrence.CreateNextSeriesOccurrence(
            occurrence.EventDate.AddMinutes(occurrence.DurationMinutes).AddMinutes(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateNextSeriesOccurrence_continues_after_a_cancelled_occurrence()
    {
        var first = CreateEvent();
        first.StartSeries(intervalWeeks: 1, totalOccurrences: 3, Now);
        first.Publish(Now);
        first.Cancel(Now);

        var second = first.CreateNextSeriesOccurrence(
            first.EventDate.AddMinutes(first.DurationMinutes).AddMinutes(1));

        second.SeriesSequence.Should().Be(2);
        second.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public void CreateNextSeriesOccurrence_rolls_forward_when_the_worker_is_late()
    {
        var first = CreateEvent();
        first.StartSeries(intervalWeeks: 1, totalOccurrences: 5, Now);
        first.Publish(Now);

        // Worker üç hafta boyunca çalışmadıysa tarih geçmişte kalmamalı.
        var lateRun = first.EventDate.AddDays(23);
        var second = first.CreateNextSeriesOccurrence(lateRun);

        second.EventDate.Should().BeAfter(lateRun);
        second.EventDate.Should().Be(first.EventDate.AddDays(28));
        second.SeriesSequence.Should().Be(2);
    }

    private static Event CreateEvent(bool isPaid = false, decimal? feeAmount = null)
    {
        var sport = Sport.Create("Football", 1, Now, "football");

        return Event.Create(
            Guid.NewGuid(),
            sport.Id,
            "Match",
            Now.AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            Now,
            isPaid: isPaid,
            feeAmount: feeAmount);
    }
}
