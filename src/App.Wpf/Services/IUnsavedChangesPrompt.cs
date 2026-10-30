using MarkdownNotes.Models;

namespace MarkdownNotes.Services;

/// <summary>Asks what to do with unsaved changes. Implemented in <c>Views/Layout</c> (it shows a styled dialog).</summary>
internal interface IUnsavedChangesPrompt
{
    /// <summary>Shows the Save / Don't save / Cancel dialog.</summary>
    /// <param name="fileName">Name of the modified document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's choice; <see cref="UnsavedChoice.Cancel"/> when the dialog is dismissed.</returns>
    Task<UnsavedChoice> ConfirmAsync(string fileName, CancellationToken cancellationToken);
}
