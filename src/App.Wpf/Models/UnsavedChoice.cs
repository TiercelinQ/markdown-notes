namespace MarkdownNotes.Models;

/// <summary>The user's answer to the unsaved-changes confirmation.</summary>
internal enum UnsavedChoice
{
    /// <summary>Save the document, then continue.</summary>
    Save,

    /// <summary>Discard the changes and continue.</summary>
    Discard,

    /// <summary>Abort the operation.</summary>
    Cancel,
}
