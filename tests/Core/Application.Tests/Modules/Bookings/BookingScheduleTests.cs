using ConferenceBooking.Core.Application.Modules.Bookings.Services;

namespace ConferenceBooking.Core.Application.Tests.Modules.Bookings;

public sealed class BookingScheduleTests
{
    [Fact]
    public void WholeBusinessDayIsAllowed()
    {
        Assert.True(BookingSchedule.IsValidPeriod(At(6), At(23)));
    }

    [Fact]
    public void LastMinuteBeforeClosingIsAllowed()
    {
        Assert.True(BookingSchedule.IsValidPeriod(At(22, 59), At(23)));
    }

    [Fact]
    public void FirstMinuteAfterOpeningIsAllowed()
    {
        Assert.True(BookingSchedule.IsValidPeriod(At(6), At(6, 1)));
    }

    [Theory]
    [InlineData(5, 59, 6, 0)]
    [InlineData(22, 59, 23, 1)]
    [InlineData(9, 0, 9, 0)]
    [InlineData(10, 0, 9, 0)]
    [InlineData(0, 0, 5, 0)]
    public void InvalidHoursAndNonPositiveDurationAreRejected(int startHour, int startMinute, int endHour, int endMinute)
    {
        Assert.False(BookingSchedule.IsValidPeriod(At(startHour, startMinute), At(endHour, endMinute)));
    }

    [Fact]
    public void OvernightBookingIsRejected()
    {
        Assert.False(BookingSchedule.IsValidPeriod(At(22), At(7).AddDays(1)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(TimeSpan.TicksPerSecond, 0)]
    [InlineData(0, TimeSpan.TicksPerSecond)]
    public void SubMinutePrecisionIsRejected(long startTicks, long endTicks)
    {
        Assert.False(BookingSchedule.IsValidPeriod(At(9).AddTicks(startTicks), At(10).AddTicks(endTicks)));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 3)]
    public void ScheduleUsesVenueTimeRegardlessOfInputOffset(int month, int kyivOffsetHours)
    {
        var start = new DateTimeOffset(2027, month, 15, 6, 0, 0, TimeSpan.FromHours(kyivOffsetHours)).ToUniversalTime();

        Assert.True(BookingSchedule.IsValidPeriod(start, start.AddHours(17)));
        Assert.Equal(new DateTime(2027, month, 15, 6, 0, 0), BookingSchedule.ToLocalTime(start));
        Assert.False(BookingSchedule.IsValidPeriod(start.AddMinutes(-1), start.AddHours(1)));
    }

    private static DateTimeOffset At(int hour, int minute = 0) => new(2027, 1, 15, hour, minute, 0, TimeSpan.FromHours(2));
}
