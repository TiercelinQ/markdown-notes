using MarkdownNotes.Models;

namespace MarkdownNotes.Services;

/// <summary>Turns a formatting action and the editor selection into a text edit. Pure: no I/O, no UI.</summary>
internal interface IMarkdownFormattingService
{
    /// <summary>Computes the edit for a formatting action.</summary>
    /// <param name="kind">The action.</param>
    /// <param name="text">Current editor text.</param>
    /// <param name="selectionStart">Selection start.</param>
    /// <param name="selectionLength">Selection length.</param>
    /// <returns>The edit to apply, with the selection to restore.</returns>
    TextEdit Apply(FormatKind kind, string text, int selectionStart, int selectionLength);
}

/// <inheritdoc />
internal sealed class MarkdownFormattingService : IMarkdownFormattingService
{
    private const string BulletPrefix = "- ";
    private const string QuotePrefix = "> ";

    /// <inheritdoc />
    public TextEdit Apply(FormatKind kind, string text, int selectionStart, int selectionLength)
    {
        ArgumentNullException.ThrowIfNull(text);

        var start = Math.Clamp(selectionStart, 0, text.Length);
        var length = Math.Clamp(selectionLength, 0, text.Length - start);

        return kind switch
        {
            FormatKind.Bold => ToggleInline(text, start, length, "**"),
            FormatKind.Italic => ToggleInline(text, start, length, "*"),
            FormatKind.Code => ApplyCode(text, start, length),
            FormatKind.Link => ApplyLink(text, start, length),
            FormatKind.Heading1 => TransformLines(text, start, length, lines => ToggleHeading(lines, 1)),
            FormatKind.Heading2 => TransformLines(text, start, length, lines => ToggleHeading(lines, 2)),
            FormatKind.Heading3 => TransformLines(text, start, length, lines => ToggleHeading(lines, 3)),
            FormatKind.BulletList => TransformLines(text, start, length, ToggleBullets),
            FormatKind.NumberedList => TransformLines(text, start, length, ToggleNumbers),
            FormatKind.Quote => TransformLines(text, start, length, ToggleQuote),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Action de format inconnue."),
        };
    }

    private static TextEdit ToggleInline(string text, int start, int length, string marker)
    {
        var width = marker.Length;
        if (length == 0)
        {
            return new TextEdit(start, 0, marker + marker, start + width, 0);
        }

        var selected = text.Substring(start, length);
        if (IsWrapped(text, start, length, marker))
        {
            return new TextEdit(start - width, length + (2 * width), selected, start - width, length);
        }

        return new TextEdit(start, length, marker + selected + marker, start + width, length);
    }

    private static bool IsWrapped(string text, int start, int length, string marker)
    {
        var width = marker.Length;
        var end = start + length;
        if (start < width || end + width > text.Length)
        {
            return false;
        }

        if (string.CompareOrdinal(text, start - width, marker, 0, width) != 0
            || string.CompareOrdinal(text, end, marker, 0, width) != 0)
        {
            return false;
        }

        // A single-character marker is not "wrapped" when it is half of a doubled one (bold around the selection).
        if (width == 1)
        {
            var before = start - 2 >= 0 ? text[start - 2] : '\0';
            var after = end + 1 < text.Length ? text[end + 1] : '\0';
            return before != marker[0] && after != marker[0];
        }

        return true;
    }

    private static TextEdit ApplyCode(string text, int start, int length)
    {
        var selected = text.Substring(start, length);
        if (!selected.Contains('\n', StringComparison.Ordinal))
        {
            return ToggleInline(text, start, length, "`");
        }

        var newline = Environment.NewLine;
        var replacement = "```" + newline + selected + newline + "```";
        return new TextEdit(start, length, replacement, start + 3 + newline.Length, length);
    }

    private static TextEdit ApplyLink(string text, int start, int length)
    {
        if (length == 0)
        {
            return new TextEdit(start, 0, "[]()", start + 1, 0);
        }

        const string Placeholder = "url";
        var selected = text.Substring(start, length);
        var urlStart = start + 1 + length + 2;
        return new TextEdit(start, length, "[" + selected + "](" + Placeholder + ")", urlStart, Placeholder.Length);
    }

    private static TextEdit TransformLines(string text, int start, int length, Func<string[], string[]> transform)
    {
        var blockStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
        var selectionEnd = start + length;
        if (length > 0 && text[selectionEnd - 1] == '\n')
        {
            selectionEnd--;
        }

        var newlineIndex = text.IndexOf('\n', selectionEnd);
        var blockEnd = newlineIndex < 0 ? text.Length : newlineIndex;

        var lines = text[blockStart..blockEnd].Split('\n');
        var replacement = string.Join('\n', transform(lines));
        return new TextEdit(blockStart, blockEnd - blockStart, replacement, blockStart, replacement.Length);
    }

    private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);

    private static string[] ToggleHeading(string[] lines, int level)
    {
        var prefix = new string('#', level) + " ";
        var content = lines.Where(line => !IsBlank(line)).ToArray();
        var remove = content.Length > 0 && content.All(line => line.StartsWith(prefix, StringComparison.Ordinal));

        return [.. lines.Select(line =>
            IsBlank(line) ? line : remove ? line[prefix.Length..] : prefix + StripHeading(line))];
    }

    private static string StripHeading(string line)
    {
        var count = 0;
        while (count < line.Length && count < 6 && line[count] == '#')
        {
            count++;
        }

        return count > 0 && count < line.Length && line[count] == ' ' ? line[(count + 1)..] : line;
    }

    private static string[] ToggleBullets(string[] lines)
    {
        var content = lines.Where(line => !IsBlank(line)).ToArray();
        var remove = content.Length > 0 && content.All(line => line.StartsWith(BulletPrefix, StringComparison.Ordinal));

        return [.. lines.Select(line =>
            IsBlank(line) ? line
            : remove ? line[BulletPrefix.Length..]
            : BulletPrefix + StripListMarker(line))];
    }

    private static string[] ToggleNumbers(string[] lines)
    {
        var content = lines.Where(line => !IsBlank(line)).ToArray();
        var remove = content.Length > 0 && content.All(line => NumberPrefixLength(line) > 0);

        var result = new string[lines.Length];
        var number = 1;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (IsBlank(line))
            {
                result[i] = line;
            }
            else if (remove)
            {
                result[i] = line[NumberPrefixLength(line)..];
            }
            else
            {
                result[i] = number.ToString(System.Globalization.CultureInfo.InvariantCulture) + ". " + StripListMarker(line);
                number++;
            }
        }

        return result;
    }

    private static string[] ToggleQuote(string[] lines)
    {
        var content = lines.Where(line => !IsBlank(line)).ToArray();
        var remove = content.Length > 0 && content.All(line => line.StartsWith(QuotePrefix, StringComparison.Ordinal));

        return [.. lines.Select(line =>
            IsBlank(line) ? line : remove ? line[QuotePrefix.Length..] : QuotePrefix + line)];
    }

    private static string StripListMarker(string line)
    {
        if (line.StartsWith(BulletPrefix, StringComparison.Ordinal))
        {
            return line[BulletPrefix.Length..];
        }

        var numberLength = NumberPrefixLength(line);
        return numberLength > 0 ? line[numberLength..] : line;
    }

    // Length of a leading "12. " marker, or 0 when the line does not start with one.
    private static int NumberPrefixLength(string line)
    {
        var digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits]))
        {
            digits++;
        }

        return digits > 0 && digits + 1 < line.Length && line[digits] == '.' && line[digits + 1] == ' '
            ? digits + 2
            : 0;
    }
}
