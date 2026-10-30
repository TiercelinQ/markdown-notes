using System.Globalization;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace MarkdownNotes.Views.Layout;

/// <inheritdoc />
internal sealed class UnsavedChangesPrompt(IContentDialogService dialogs) : IUnsavedChangesPrompt
{
    /// <inheritdoc />
    public async Task<UnsavedChoice> ConfirmAsync(string fileName, CancellationToken cancellationToken)
    {
        var options = new SimpleContentDialogCreateOptions
        {
            Title = Strings.Unsaved_Title,
            Content = new System.Windows.Controls.TextBlock
            {
                Text = string.Format(CultureInfo.CurrentCulture, Strings.Unsaved_Message, fileName),
                Style = (System.Windows.Style)System.Windows.Application.Current.FindResource("DialogMessageStyle"),
            },
            PrimaryButtonText = Strings.Unsaved_Save,
            SecondaryButtonText = Strings.Unsaved_Discard,
            CloseButtonText = Strings.Unsaved_Cancel,
        };

        var result = await dialogs.ShowSimpleDialogAsync(options, cancellationToken);
        return result switch
        {
            ContentDialogResult.Primary => UnsavedChoice.Save,
            ContentDialogResult.Secondary => UnsavedChoice.Discard,
            _ => UnsavedChoice.Cancel,
        };
    }
}
