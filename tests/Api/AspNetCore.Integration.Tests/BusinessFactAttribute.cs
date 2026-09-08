using System.Runtime.CompilerServices;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

public sealed class BusinessFactAttribute : FactAttribute
{
    public BusinessFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = "Set CONFERENCE_BOOKING_TEST_CONNECTION_STRING to a dedicated PostgreSQL test database.";
        SkipType = typeof(BusinessAppFactory);
        SkipUnless = nameof(BusinessAppFactory.IsConfigured);
    }
}
