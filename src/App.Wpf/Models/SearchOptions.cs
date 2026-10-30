namespace MarkdownNotes.Models;

/// <summary>Parameters of a find or replace operation.</summary>
/// <param name="Query">Text to find.</param>
/// <param name="Replacement">Text that replaces a match.</param>
/// <param name="MatchCase">True for a case-sensitive search.</param>
internal sealed record SearchOptions(string Query, string Replacement, bool MatchCase);
