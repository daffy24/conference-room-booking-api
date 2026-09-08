namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Reports;

[Trait("Category", "Postgresql")]
public sealed class RevenueReportTests(BusinessAppFactory factory) : ReportEndpointTestsBase(factory), IClassFixture<BusinessAppFactory>
{
    [BusinessFact]
    public async Task RevenueUsesBookingSnapshotsWithoutMultiplyingBookingsByServices()
    {
        var room = await SeedRoomAsync();
        var projector = room.Services.Single(service => service.Name == "Projector");
        await SeedBookingsAsync(
            CreateBooking(room, WinterStart(15), 60, 2000m, room.Services.ToArray()),
            CreateBooking(room, WinterStart(16, 14), 90, 3000m, projector));
        using var admin = CreateClient("Admin");

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-01-15", "2024-01-17", room.Id));

        AssertRevenue(revenue, 2, 5000m, 1300m);
        Assert.Equal(3150m, revenue.GetProperty("averageBookingValue").GetDecimal());
        Assert.Equal("2024-01-15", revenue.GetProperty("from").GetString());
        Assert.Equal("2024-01-17", revenue.GetProperty("to").GetString());
        Assert.Equal("UAH", revenue.GetProperty("currency").GetString());
        Assert.Equal("Europe/Kyiv", revenue.GetProperty("timeZone").GetString());
        var days = revenue.GetProperty("days").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2024-01-15", "2024-01-16", "2024-01-17" }, days.Select(day => day.GetProperty("date").GetString()));
        AssertRevenue(days[0], 1, 2000m, 800m);
        AssertRevenue(days[1], 1, 3000m, 500m);
        AssertRevenue(days[2], 0, 0m, 0m);
    }

    [BusinessFact]
    public async Task InclusiveReportDatesFilterBookingStartInsteadOfCreationTime()
    {
        var room = await SeedRoomAsync();
        var firstDay = CreateBooking(room, WinterStart(15, 6), 60, 1800m);
        var lastDay = CreateBooking(room, WinterStart(16, 22), 60, 1600m);
        var outsideRange = CreateBooking(room, WinterStart(17), 60, 2000m);
        outsideRange.CreatedAt = WinterStart(15).ToUniversalTime();
        await SeedBookingsAsync(
            CreateBooking(room, WinterStart(14, 22), 60, 1600m),
            firstDay,
            lastDay,
            outsideRange);
        using var admin = CreateClient("Admin");

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-01-15", "2024-01-16", room.Id));

        AssertRevenue(revenue, 2, 3400m, 0m);
        var days = revenue.GetProperty("days").EnumerateArray().ToArray();
        AssertRevenue(days[0], 1, 1800m, 0m);
        AssertRevenue(days[1], 1, 1600m, 0m);
    }

    [BusinessFact]
    public async Task RevenueGroupsKyivDatesAcrossDaylightSavingTransition()
    {
        var room = await SeedRoomAsync();
        // Kyiv changes from UTC+2 to UTC+3 on March 31, 2024; both meetings start at 06:00 locally.
        await SeedBookingsAsync(
            CreateBooking(room, new DateTimeOffset(2024, 3, 30, 6, 0, 0, TimeSpan.FromHours(2)), 60, 1800m),
            CreateBooking(room, new DateTimeOffset(2024, 3, 31, 6, 0, 0, TimeSpan.FromHours(3)), 60, 1800m),
            CreateBooking(room, new DateTimeOffset(2024, 4, 1, 6, 0, 0, TimeSpan.FromHours(3)), 60, 1800m));
        using var admin = CreateClient("Admin");

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-03-30", "2024-03-31", room.Id));

        AssertRevenue(revenue, 2, 3600m, 0m);
        var days = revenue.GetProperty("days").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2024-03-30", "2024-03-31" }, days.Select(day => day.GetProperty("date").GetString()));
        Assert.All(days, day => AssertRevenue(day, 1, 1800m, 0m));
    }

    [BusinessFact]
    public async Task RevenueIncludesFutureBookingsAndOnlyTheRequestedRoom()
    {
        var room = await SeedRoomAsync();
        var otherRoom = await SeedRoomAsync();
        var startsAt = FutureStart(10);
        await SeedBookingsAsync(
            CreateBooking(room, startsAt, 60, 2000m),
            CreateBooking(otherRoom, startsAt, 60, 2000m));
        using var admin = CreateClient("Admin");
        var day = startsAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", day, day, room.Id));

        AssertRevenue(revenue, 1, 2000m, 0m);
        Assert.Equal(2000m, revenue.GetProperty("averageBookingValue").GetDecimal());
        Assert.Single(revenue.GetProperty("days").EnumerateArray());
    }

    [BusinessFact]
    public async Task FullLeapYearIsAcceptedAndContainsEveryDateOnce()
    {
        using var admin = CreateClient("Admin");

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-01-01", "2024-12-31", Guid.NewGuid()));

        var days = revenue.GetProperty("days").EnumerateArray().ToArray();
        Assert.Equal(366, days.Length);
        Assert.Equal("2024-01-01", days[0].GetProperty("date").GetString());
        Assert.Equal("2024-12-31", days[^1].GetProperty("date").GetString());
        Assert.Equal(366, days.Select(day => day.GetProperty("date").GetString()).Distinct().Count());
        AssertRevenue(revenue, 0, 0m, 0m);
    }
}
