using System.Net;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Reports;

[Trait("Category", "Postgresql")]
public sealed class ReportAccessAndValidationTests(BusinessAppFactory factory) : ReportEndpointTestsBase(factory), IClassFixture<BusinessAppFactory>
{
    [BusinessFact]
    public async Task ReportsRequireAdministratorRole()
    {
        using var anonymous = CreateAnonymousClient();
        using var customer = CreateClient();
        using var admin = CreateClient("Admin");

        foreach (var report in new[] { "revenue", "rooms", "services" })
        {
            var path = ReportPath(report, "2024-01-01", "2024-01-02", Guid.NewGuid());
            using var anonymousResponse = await anonymous.GetAsync(path, CancellationToken);
            using var customerResponse = await customer.GetAsync(path, CancellationToken);
            using var adminResponse = await admin.GetAsync(path, CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        }
    }

    [BusinessFact]
    public async Task InvalidOrMissingReportDatesReturnValidationProblems()
    {
        using var admin = CreateClient("Admin");
        var invalidQueries = new[]
        {
            "",
            "from=2024-01-01",
            "to=2024-01-01",
            "from=0001-01-01&to=0001-01-02",
            "from=2024-01-01&to=0001-01-01",
            "from=2024-01-02&to=2024-01-01",
            "from=2024-01-01&to=2025-01-01",
            "from=invalid&to=2024-01-01",
            "from=2024-01-01&to=2024-01-01&roomId=00000000-0000-0000-0000-000000000000",
            "from=2024-01-01&to=2024-01-01&roomId=invalid",
        };

        foreach (var report in new[] { "revenue", "rooms", "services" })
        {
            foreach (var query in invalidQueries)
            {
                using var response = await admin.GetAsync($"/api/reports/{report}?{query}", CancellationToken);
                await AssertProblemAsync(response, HttpStatusCode.BadRequest);
            }
        }
    }

    [BusinessFact]
    public async Task ReportPaginationRejectsUnboundedAndNonPositiveValues()
    {
        using var admin = CreateClient("Admin");
        var invalidPages = new[] { "page=0", "page=-1", "page=2147483647", "pageSize=0", "pageSize=-1", "pageSize=101" };

        foreach (var report in new[] { "rooms", "services" })
        {
            foreach (var pagination in invalidPages)
            {
                using var response = await admin.GetAsync($"/api/reports/{report}?from=2024-01-01&to=2024-01-02&{pagination}", CancellationToken);
                await AssertProblemAsync(response, HttpStatusCode.BadRequest);
            }
        }
    }

    [BusinessFact]
    public async Task UnknownRoomProducesEmptyReportsInsteadOfNotFound()
    {
        using var admin = CreateClient("Admin");
        var unknownRoomId = Guid.NewGuid();
        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-01-01", "2024-01-02", unknownRoomId));

        AssertRevenue(revenue, 0, 0m, 0m);
        Assert.Equal(0m, revenue.GetProperty("averageBookingValue").GetDecimal());
        Assert.Equal(2, revenue.GetProperty("days").GetArrayLength());
        Assert.All(revenue.GetProperty("days").EnumerateArray(), day => AssertRevenue(day, 0, 0m, 0m));

        foreach (var report in new[] { "rooms", "services" })
        {
            var result = await ReadReportAsync(admin, ReportPath(report, "2024-01-01", "2024-01-02", unknownRoomId));
            Assert.Equal(0, result.GetProperty("totalCount").GetInt32());
            Assert.Empty(result.GetProperty("items").EnumerateArray());
        }
    }
}
