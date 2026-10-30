namespace MarkdownNotes.Models;

/// <summary>Which panes of the editing area are visible.</summary>
internal enum ViewMode
{
    /// <summary>Editor and preview side by side.</summary>
    Split,

    /// <summary>Editor only.</summary>
    EditorOnly,

    /// <summary>Preview only.</summary>
    PreviewOnly,
}
