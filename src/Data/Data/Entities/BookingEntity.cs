using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConferenceBooking.Data.Entities;

/// <summary>
/// Represents a reservation with the rental terms captured when it was created.
/// </summary>
[Table("bookings")]
public sealed class BookingEntity
{
    /// <summary>
    /// The unique booking identifier.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The reserved room identifier.
    /// </summary>
    [Column("room_id")]
    public Guid RoomId { get; set; }

    /// <summary>
    /// The authenticated subject that owns the booking.
    /// </summary>
    [Column("user_id")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// The room name at the time of booking.
    /// </summary>
    [Column("room_name")]
    public string RoomName { get; set; } = null!;

    /// <summary>
    /// The standard hourly rate used to calculate this booking.
    /// </summary>
    [Column("base_hourly_rate")]
    public decimal BaseHourlyRate { get; set; }

    /// <summary>
    /// The inclusive start of the reserved interval, stored in UTC.
    /// </summary>
    [Column("starts_at")]
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// The exclusive end of the reserved interval, stored in UTC.
    /// </summary>
    [Column("ends_at")]
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>
    /// The time the booking was created, stored in UTC.
    /// </summary>
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The calculated room rental cost in Ukrainian hryvnias.
    /// </summary>
    [Column("rental_cost")]
    public decimal RentalCost { get; set; }

    /// <summary>
    /// The total selected service cost in Ukrainian hryvnias.
    /// </summary>
    [Column("services_cost")]
    public decimal ServicesCost { get; set; }

    /// <summary>
    /// The total payable amount in Ukrainian hryvnias.
    /// </summary>
    [Column("total_cost")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// The room associated with this booking, including a soft-deleted room.
    /// </summary>
    public RoomEntity Room { get; set; } = null!;

    /// <summary>
    /// The selected services and their prices at the time of booking.
    /// </summary>
    public ICollection<BookingServiceEntity> Services { get; set; } = [];
}
