namespace MarkdownNotes.Models;

/// <summary>A Markdown formatting action applied to the editor selection.</summary>
internal enum FormatKind
{
    /// <summary>Bold (<c>**text**</c>).</summary>
    Bold,

    /// <summary>Italic (<c>*text*</c>).</summary>
    Italic,

    /// <summary>Level 1 heading.</summary>
    Heading1,

    /// <summary>Level 2 heading.</summary>
    Heading2,

    /// <summary>Level 3 heading.</summary>
    Heading3,

    /// <summary>Bulleted list.</summary>
    BulletList,

    /// <summary>Numbered list.</summary>
    NumberedList,

    /// <summary>Link (<c>[text](url)</c>).</summary>
    Link,

    /// <summary>Inline code or fenced block when the selection spans several lines.</summary>
    Code,

    /// <summary>Block quote.</summary>
    Quote,
}
