namespace MarkdownNotes.Helpers;

/// <summary>Converts a character index into a 1-based line and column.</summary>
internal static class LineColumn
{
    /// <summary>Computes the line and column of an index in a text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="index">Character index; clamped to the text bounds.</param>
    /// <returns>The 1-based line and column.</returns>
    public static (int Line, int Column) FromIndex(string text, int index)
    {
        ArgumentNullException.ThrowIfNull(text);

        var clamped = Math.Clamp(index, 0, text.Length);
        var line = 1;
        var lineStart = 0;

        for (var i = 0; i < clamped; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, clamped - lineStart + 1);
    }
}
