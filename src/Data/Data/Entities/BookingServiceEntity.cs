using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConferenceBooking.Data.Entities;

/// <summary>
/// Preserves a selected service and its price independently of later room changes.
/// </summary>
[Table("booking_services")]
public sealed class BookingServiceEntity
{
    /// <summary>
    /// The unique booked service identifier.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The booking that includes this service.
    /// </summary>
    [Column("booking_id")]
    public Guid BookingId { get; set; }

    /// <summary>
    /// The original room service identifier; it remains valid in history after removal.
    /// </summary>
    [Column("service_id")]
    public Guid ServiceId { get; set; }

    /// <summary>
    /// The service name at the time of booking.
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The agreed service price in Ukrainian hryvnias.
    /// </summary>
    [Column("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// The booking associated with this service snapshot.
    /// </summary>
    public BookingEntity Booking { get; set; } = null!;
}
