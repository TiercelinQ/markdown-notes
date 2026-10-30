namespace MarkdownNotes.Models;

/// <summary>A path is empty, malformed, of an unsupported type or outside the allowed root.</summary>
/// <param name="message">Technical description, logged but never shown raw.</param>
internal sealed class InvalidDocumentPathException(string message) : Exception(message);

/// <summary>A document exceeds the size the application accepts to load.</summary>
/// <param name="message">Technical description, logged but never shown raw.</param>
internal sealed class DocumentTooLargeException(string message) : Exception(message);
