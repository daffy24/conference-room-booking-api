using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConferenceBooking.Data.Entities;

/// <summary>
/// Represents a conference room and its current rental terms.
/// </summary>
[Table("rooms")]
public sealed class RoomEntity
{
    /// <summary>
    /// The unique room identifier.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The name displayed to customers.
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The maximum number of people the room accommodates.
    /// </summary>
    [Column("capacity")]
    public int Capacity { get; set; }

    /// <summary>
    /// The standard hourly rental price in Ukrainian hryvnias.
    /// </summary>
    [Column("base_hourly_rate")]
    public decimal BaseHourlyRate { get; set; }

    /// <summary>
    /// Indicates that the room is unavailable while its booking history is retained.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Changes with every room update or booking to detect concurrent changes.
    /// </summary>
    [Column("version")]
    public Guid Version { get; set; }

    /// <summary>
    /// The services currently available for this room.
    /// </summary>
    public ICollection<RoomServiceEntity> Services { get; set; } = [];
}
