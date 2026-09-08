using System.Globalization;

namespace ConferenceBooking.Api.AspNetCore.Binding;

internal static class DateTimeOffsetParser
{
    public const string ErrorMessage = "Use an ISO 8601 timestamp with an explicit UTC offset, for example 2026-09-10T10:00:00+03:00 or 2026-09-10T07:00:00Z.";

    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mmzzz",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd'T'HH:mm'Z'",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
    ];

    // Requiring an offset prevents the same request from changing meaning between local and Docker time zones.
    public static bool TryParse(string? value, out DateTimeOffset result) =>
        DateTimeOffset.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out result);
}
