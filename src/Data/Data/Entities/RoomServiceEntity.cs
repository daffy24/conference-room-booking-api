using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConferenceBooking.Data.Entities;

/// <summary>
/// Represents an optional service offered with a particular room.
/// </summary>
[Table("room_services")]
public sealed class RoomServiceEntity
{
    /// <summary>
    /// The unique identifier of the service offered by the room.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The room that offers this service.
    /// </summary>
    [Column("room_id")]
    public Guid RoomId { get; set; }

    /// <summary>
    /// The service name displayed to customers.
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The service price in Ukrainian hryvnias.
    /// </summary>
    [Column("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// The room associated with this service.
    /// </summary>
    public RoomEntity Room { get; set; } = null!;
}
