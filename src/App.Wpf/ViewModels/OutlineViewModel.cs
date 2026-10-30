using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.ViewModels;

/// <summary>Heading tree of the current document, refreshed with a debounce after each edit.</summary>
internal sealed partial class OutlineViewModel : ObservableObject, IDisposable
{
    private readonly EditorViewModel _editor;
    private readonly IOutlineService _outline;
    private readonly IFeedbackService _feedback;
    private readonly ILogger<OutlineViewModel> _logger;
    private CancellationTokenSource? _updateCts;

    /// <summary>Initializes a new instance of the <see cref="OutlineViewModel"/> class.</summary>
    /// <param name="editor">The editor whose text is outlined and which is moved to a heading.</param>
    /// <param name="outline">Heading extraction service.</param>
    /// <param name="localization">Observable localization service.</param>
    /// <param name="feedback">Snackbar feedback.</param>
    /// <param name="logger">Logger.</param>
    public OutlineViewModel(
        EditorViewModel editor,
        IOutlineService outline,
        ILocalizationService localization,
        IFeedbackService feedback,
        ILogger<OutlineViewModel> logger)
    {
        _editor = editor;
        _outline = outline;
        _feedback = feedback;
        _logger = logger;
        Loc = localization;

        _editor.TextChanged += OnEditorTextChanged;
    }

    /// <summary>Gets the localized strings.</summary>
    public ILocalizationService Loc { get; }

    /// <summary>Gets the top-level headings.</summary>
    public ObservableCollection<HeadingNodeViewModel> Roots { get; } = [];

    /// <summary>Gets a value indicating whether the document has at least one heading.</summary>
    public bool HasHeadings => Roots.Count > 0;

    /// <summary>Gets or sets a value indicating whether the heading list is open. The title stays visible when it is closed.</summary>
    [ObservableProperty]
    private bool _isPaneOpen = true;

    /// <summary>Rebuilds the tree from the current editor text.</summary>
    /// <param name="cancellationToken">Cancellation token; cancelled when a newer edit arrives.</param>
    /// <returns>A task completing once the tree is replaced.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var headings = await _outline.ExtractAsync(_editor.Text, cancellationToken);

        Roots.Clear();
        foreach (var heading in headings)
        {
            Roots.Add(new HeadingNodeViewModel(heading, this));
        }

        OnPropertyChanged(nameof(HasHeadings));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _editor.TextChanged -= OnEditorTextChanged;
        _updateCts?.Cancel();
        _updateCts?.Dispose();
    }

    [RelayCommand]
    private void GoToHeading(HeadingNodeViewModel? heading)
    {
        if (heading is not null)
        {
            _editor.NavigateToHeading(
                heading.Index,
                new TextEdit(heading.Offset, 0, null, heading.Offset, heading.Length));
        }
    }

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _updateCts?.Cancel();
        _updateCts?.Dispose();
        _updateCts = new CancellationTokenSource();
        _ = RunUpdateAsync(_updateCts.Token);
    }

    private async Task RunUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AppConfig.PreviewDebounceMs, cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer edit superseded this update.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Echec de la construction du plan");
            _feedback.Show(new ResultError(FeedbackType.Danger, Strings.Error_OutlineFailed));
        }
    }
}
