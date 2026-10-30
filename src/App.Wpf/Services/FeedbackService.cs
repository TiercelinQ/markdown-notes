using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MarkdownNotes.Services;

/// <summary>Surfaces a <see cref="ResultError"/> as a snackbar: the single place where feedback types map to appearance.</summary>
internal interface IFeedbackService
{
    /// <summary>Shows a snackbar for the given message.</summary>
    /// <param name="error">Message and category.</param>
    void Show(ResultError error);
}

/// <inheritdoc />
internal sealed class FeedbackService(ISnackbarService snackbar) : IFeedbackService
{
    private static readonly TimeSpan SuccessTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan WarningTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan PersistentTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <inheritdoc />
    public void Show(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var message = error.Description is null ? error.Message : error.Message + "\n" + error.Description;
        snackbar.Show(Title(error.Type), message, Appearance(error.Type), new SymbolIcon(Symbol(error.Type)), Timeout(error.Type));
    }

    private static string Title(FeedbackType type) => type switch
    {
        FeedbackType.Success => Strings.Feedback_Success,
        FeedbackType.Warning => Strings.Feedback_Warning,
        FeedbackType.Danger => Strings.Feedback_Danger,
        _ => Strings.Feedback_Info,
    };

    private static ControlAppearance Appearance(FeedbackType type) => type switch
    {
        FeedbackType.Success => ControlAppearance.Success,
        FeedbackType.Warning => ControlAppearance.Caution,
        FeedbackType.Danger => ControlAppearance.Danger,
        _ => ControlAppearance.Info,
    };

    private static SymbolRegular Symbol(FeedbackType type) => type switch
    {
        FeedbackType.Success => SymbolRegular.CheckmarkCircle24,
        FeedbackType.Warning => SymbolRegular.Warning24,
        FeedbackType.Danger => SymbolRegular.DismissCircle24,
        _ => SymbolRegular.Info24,
    };

    // Durations: layout.md section 5. A danger snackbar stays until dismissed.
    private static TimeSpan Timeout(FeedbackType type) => type switch
    {
        FeedbackType.Warning => WarningTimeout,
        FeedbackType.Danger => PersistentTimeout,
        _ => SuccessTimeout,
    };
}
