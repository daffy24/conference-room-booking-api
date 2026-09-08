namespace ConferenceBooking.Core.Application.Exceptions;

/// <summary>
/// Indicates that a requested resource is unavailable to the caller.
/// </summary>
/// <param name="message">The safe, public explanation.</param>
public sealed class NotFoundException(string message) : Exception(message);
