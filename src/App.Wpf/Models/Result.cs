namespace MarkdownNotes.Models;

/// <summary>Visual category of a user-facing feedback message.</summary>
internal enum FeedbackType
{
    /// <summary>An operation completed.</summary>
    Success,

    /// <summary>A neutral information.</summary>
    Info,

    /// <summary>A recoverable problem or a rejected input.</summary>
    Warning,

    /// <summary>A failure the user must acknowledge.</summary>
    Danger,
}

/// <summary>Structured failure returned by a service and surfaced as a snackbar.</summary>
/// <param name="Type">Feedback category.</param>
/// <param name="Message">Short message, already localized.</param>
/// <param name="Description">Optional secondary line, already localized.</param>
internal sealed record ResultError(FeedbackType Type, string Message, string? Description = null);

/// <summary>Empty payload for operations that return nothing.</summary>
internal sealed record Unit
{
    /// <summary>Gets the single value of the type.</summary>
    public static Unit Value { get; } = new();
}

/// <summary>Outcome of a service call: either data or a <see cref="ResultError"/>.</summary>
/// <typeparam name="T">Payload type on success.</typeparam>
internal readonly record struct Result<T>
{
    /// <summary>Gets a value indicating whether the call succeeded.</summary>
    public bool Ok { get; private init; }

    /// <summary>Gets the payload when <see cref="Ok"/> is true.</summary>
    public T? Data { get; private init; }

    /// <summary>Gets the error when <see cref="Ok"/> is false.</summary>
    public ResultError? Error { get; private init; }

    /// <summary>Creates a successful result.</summary>
    /// <param name="data">The payload.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T data) => new() { Ok = true, Data = data };

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error to surface.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(ResultError error) => new() { Ok = false, Error = error };
}
