using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using Microsoft.Win32;

namespace MarkdownNotes.Views.Layout;

/// <inheritdoc />
internal sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? PickOpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.Dialog_OpenTitle,
            Filter = Strings.Dialog_Filter_Markdown,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? PickSaveFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Dialog_SaveTitle,
            Filter = Strings.Dialog_Filter_Markdown,
            FileName = suggestedName,
            DefaultExt = ".md",
            AddExtension = true,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? PickFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = Strings.Dialog_FolderTitle,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
