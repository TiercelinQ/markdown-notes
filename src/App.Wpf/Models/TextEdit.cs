namespace MarkdownNotes.Models;

/// <summary>
/// A change requested on the editor text. Coordinates of the replaced range refer to the text
/// before the edit; the resulting selection refers to the text after the edit.
/// </summary>
/// <param name="Start">Start index of the replaced range.</param>
/// <param name="Length">Length of the replaced range.</param>
/// <param name="Replacement">New text for the range, or null to only select the range.</param>
/// <param name="SelectionStart">Selection start after the edit.</param>
/// <param name="SelectionLength">Selection length after the edit.</param>
internal sealed record TextEdit(int Start, int Length, string? Replacement, int SelectionStart, int SelectionLength);
