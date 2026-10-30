namespace MarkdownNotes.Models;

/// <summary>Location of a match in the editor text.</summary>
/// <param name="Start">Start index of the match.</param>
/// <param name="Length">Length of the match.</param>
internal sealed record SearchMatch(int Start, int Length);
