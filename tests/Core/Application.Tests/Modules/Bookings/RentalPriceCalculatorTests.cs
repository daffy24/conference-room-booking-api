using ConferenceBooking.Core.Application.Modules.Bookings.Services;

namespace ConferenceBooking.Core.Application.Tests.Modules.Bookings;

public sealed class RentalPriceCalculatorTests
{
    private readonly RentalPriceCalculator _calculator = new();

    [Theory]
    [InlineData(6, 9, 5400)]
    [InlineData(9, 12, 6000)]
    [InlineData(12, 14, 4600)]
    [InlineData(14, 18, 8000)]
    [InlineData(18, 23, 8000)]
    public void EachTariffAppliesToItsEntirePeriod(int startHour, int endHour, int expected)
    {
        var actual = _calculator.Calculate(2000m, At(startHour), At(endHour));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(8, 30, 9, 30, 1900)]
    [InlineData(11, 30, 12, 30, 2150)]
    [InlineData(13, 30, 14, 30, 2150)]
    [InlineData(17, 30, 18, 30, 1800)]
    [InlineData(10, 0, 14, 0, 8600)]
    [InlineData(6, 0, 23, 0, 32000)]
    public void CrossingTariffsChargesEachPartAtItsOwnRate(int startHour, int startMinute, int endHour, int endMinute, int expected)
    {
        var actual = _calculator.Calculate(2000m, At(startHour, startMinute), At(endHour, endMinute));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PartialHourIsProratedByMinutes()
    {
        var actual = _calculator.Calculate(1500m, At(10, 15), At(10, 45));

        Assert.Equal(750m, actual);
    }

    [Fact]
    public void FinalSubtotalIsRoundedOnceAfterCombiningTariffs()
    {
        // Separately rounded fragments would incorrectly total 0.06 instead of 0.05.
        var actual = _calculator.Calculate(1.50m, At(11, 59), At(12, 1));

        Assert.Equal(0.05m, actual);
    }

    [Fact]
    public void HalfKopeckIsRoundedAwayFromZero()
    {
        var actual = _calculator.Calculate(0.30m, At(10), At(10, 1));

        Assert.Equal(0.01m, actual);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 3)]
    public void PricingUsesKyivTariffsForBothWinterAndSummer(int month, int kyivOffsetHours)
    {
        var start = new DateTimeOffset(2027, month, 15, 12, 0, 0, TimeSpan.FromHours(kyivOffsetHours)).ToUniversalTime();

        var actual = _calculator.Calculate(2000m, start, start.AddHours(1));

        Assert.Equal(2300m, actual);
    }

    [Fact]
    public void DifferentInputOffsetsRepresentingTheSameInstantsHaveTheSamePrice()
    {
        var start = At(11, 30);
        var end = At(14, 30);

        var actual = _calculator.Calculate(2000m, start.ToOffset(TimeSpan.FromHours(-5)), end.ToOffset(TimeSpan.FromHours(9)));

        Assert.Equal(6600m, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveHourlyRateIsRejected(int hourlyRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(hourlyRate, At(9), At(10)));
    }

    [Fact]
    public void UnsupportedPeriodIsRejectedInsteadOfPartiallyPriced()
    {
        Assert.Throws<ArgumentException>(() => _calculator.Calculate(2000m, At(5), At(7)));
    }

    private static DateTimeOffset At(int hour, int minute = 0) => new(2027, 1, 15, hour, minute, 0, TimeSpan.FromHours(2));
}
