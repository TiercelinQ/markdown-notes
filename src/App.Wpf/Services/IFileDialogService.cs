namespace MarkdownNotes.Services;

/// <summary>Shows the Windows file and folder pickers. Implemented in <c>Views/Layout</c> (it needs a WPF owner window).</summary>
internal interface IFileDialogService
{
    /// <summary>Asks the user for a document to open.</summary>
    /// <returns>The chosen path, or null when cancelled.</returns>
    string? PickOpenFile();

    /// <summary>Asks the user where to save a document.</summary>
    /// <param name="suggestedName">File name proposed in the dialog.</param>
    /// <returns>The chosen path, or null when cancelled.</returns>
    string? PickSaveFile(string suggestedName);

    /// <summary>Asks the user for a folder to explore.</summary>
    /// <returns>The chosen folder, or null when cancelled.</returns>
    string? PickFolder();
}
