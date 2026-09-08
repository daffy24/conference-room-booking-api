namespace ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;

/// <summary>Inclusive Kyiv calendar dates used to select bookings by their scheduled start.</summary>
public class ReportPeriodModel
{
    /// <summary>First date to include, in YYYY-MM-DD format.</summary>
    public DateOnly From { get; init; }

    /// <summary>Last date to include, in YYYY-MM-DD format; the period may contain at most 366 days.</summary>
    public DateOnly To { get; init; }

    /// <summary>Optional room filter, including rooms removed from the catalogue.</summary>
    public Guid? RoomId { get; init; }
}
