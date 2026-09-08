namespace ConferenceBooking.Core.Application.Exceptions;

/// <summary>
/// Indicates that the requested change conflicts with the current business state.
/// </summary>
/// <param name="message">The safe, public explanation.</param>
public sealed class ConflictException(string message) : Exception(message);
