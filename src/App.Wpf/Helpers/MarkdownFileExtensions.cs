using System.IO;

namespace MarkdownNotes.Helpers;

/// <summary>Recognizes the files the application treats as Markdown documents.</summary>
internal static class MarkdownFileExtensions
{
    /// <summary>Tells whether a path has a Markdown extension (case-insensitive).</summary>
    /// <param name="path">A file path or name.</param>
    /// <returns>True when the extension is one of <see cref="AppConfig.MarkdownExtensions"/>.</returns>
    public static bool IsMarkdown(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return AppConfig.MarkdownExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
