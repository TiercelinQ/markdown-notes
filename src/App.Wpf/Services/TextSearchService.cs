using System.Text;
using MarkdownNotes.Models;

namespace MarkdownNotes.Services;

/// <summary>Find and replace on a text. Pure: no I/O, no UI.</summary>
internal interface ITextSearchService
{
    /// <summary>Finds the first match at or after an index, wrapping to the start.</summary>
    /// <param name="text">Text to search.</param>
    /// <param name="options">Query and case option.</param>
    /// <param name="fromIndex">Index to start from.</param>
    /// <returns>The match, or null when there is none.</returns>
    SearchMatch? FindNext(string text, SearchOptions options, int fromIndex);

    /// <summary>Finds the last match that ends at or before an index, wrapping to the end.</summary>
    /// <param name="text">Text to search.</param>
    /// <param name="options">Query and case option.</param>
    /// <param name="beforeIndex">Index to search before.</param>
    /// <returns>The match, or null when there is none.</returns>
    SearchMatch? FindPrevious(string text, SearchOptions options, int beforeIndex);

    /// <summary>Replaces the selection when it is a match, then selects the next match.</summary>
    /// <param name="text">Current text.</param>
    /// <param name="options">Query, replacement and case option.</param>
    /// <param name="selectionStart">Selection start.</param>
    /// <param name="selectionLength">Selection length.</param>
    /// <returns>The edit, or null when the selection is not a match.</returns>
    TextEdit? ReplaceCurrent(string text, SearchOptions options, int selectionStart, int selectionLength);

    /// <summary>Replaces every match.</summary>
    /// <param name="text">Current text.</param>
    /// <param name="options">Query, replacement and case option.</param>
    /// <returns>The edit covering the whole text and the number of replacements; a null edit when nothing matched.</returns>
    (TextEdit? Edit, int Count) ReplaceAll(string text, SearchOptions options);
}

/// <inheritdoc />
internal sealed class TextSearchService : ITextSearchService
{
    /// <inheritdoc />
    public SearchMatch? FindNext(string text, SearchOptions options, int fromIndex)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.Query))
        {
            return null;
        }

        var comparison = Comparison(options);
        var from = Math.Clamp(fromIndex, 0, text.Length);
        var index = text.IndexOf(options.Query, from, comparison);
        if (index < 0 && from > 0)
        {
            index = text.IndexOf(options.Query, 0, comparison);
        }

        return index < 0 ? null : new SearchMatch(index, options.Query.Length);
    }

    /// <inheritdoc />
    public SearchMatch? FindPrevious(string text, SearchOptions options, int beforeIndex)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.Query))
        {
            return null;
        }

        var comparison = Comparison(options);
        var before = Math.Clamp(beforeIndex, 0, text.Length);
        var index = text.AsSpan(0, before).LastIndexOf(options.Query.AsSpan(), comparison);
        if (index < 0 && before < text.Length)
        {
            index = text.AsSpan().LastIndexOf(options.Query.AsSpan(), comparison);
        }

        return index < 0 ? null : new SearchMatch(index, options.Query.Length);
    }

    /// <inheritdoc />
    public TextEdit? ReplaceCurrent(string text, SearchOptions options, int selectionStart, int selectionLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.Query))
        {
            return null;
        }

        var start = Math.Clamp(selectionStart, 0, text.Length);
        var length = Math.Clamp(selectionLength, 0, text.Length - start);
        if (length != options.Query.Length
            || !string.Equals(text.Substring(start, length), options.Query, Comparison(options)))
        {
            return null;
        }

        var newText = string.Concat(text.AsSpan(0, start), options.Replacement, text.AsSpan(start + length));
        var caret = start + options.Replacement.Length;
        var next = FindNext(newText, options, caret);

        return next is null
            ? new TextEdit(start, length, options.Replacement, caret, 0)
            : new TextEdit(start, length, options.Replacement, next.Start, next.Length);
    }

    /// <inheritdoc />
    public (TextEdit? Edit, int Count) ReplaceAll(string text, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.Query))
        {
            return (null, 0);
        }

        var comparison = Comparison(options);
        var builder = new StringBuilder(text.Length);
        var position = 0;
        var count = 0;

        while (true)
        {
            var index = text.IndexOf(options.Query, position, comparison);
            if (index < 0)
            {
                break;
            }

            builder.Append(text, position, index - position).Append(options.Replacement);
            position = index + options.Query.Length;
            count++;
        }

        if (count == 0)
        {
            return (null, 0);
        }

        builder.Append(text, position, text.Length - position);
        return (new TextEdit(0, text.Length, builder.ToString(), 0, 0), count);
    }

    private static StringComparison Comparison(SearchOptions options) =>
        options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
