using ConferenceBooking.Core.Application.Modules.Reports.Models;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Core.Application.Modules.Reports.Adapters;

[Mapper]
internal static partial class ReportsAdapter
{
    [MapProperty(nameof(RoomReportRow.BookedMinutes), nameof(RoomReportModel.BookedHours), Use = nameof(ToHours))]
    internal static partial RoomReportModel ToModel(this RoomReportRow row);

    internal static partial ServiceReportModel ToModel(this ServiceReportRow row);

    [UserMapping(Default = false)]
    private static decimal ToHours(decimal minutes) =>
        decimal.Round(minutes / 60m, 2, MidpointRounding.AwayFromZero);
}
