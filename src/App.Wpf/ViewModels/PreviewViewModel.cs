using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.ViewModels;

/// <summary>Rendered HTML of the current document, refreshed with a debounce while the pane is visible.</summary>
internal sealed partial class PreviewViewModel : ObservableObject, IDisposable
{
    private readonly EditorViewModel _editor;
    private readonly IMarkdownRenderService _render;
    private readonly IFeedbackService _feedback;
    private readonly ILogger<PreviewViewModel> _logger;
    private CancellationTokenSource? _updateCts;
    private string? _lastErrorMessage;

    /// <summary>Initializes a new instance of the <see cref="PreviewViewModel"/> class.</summary>
    /// <param name="editor">The editor whose text is rendered.</param>
    /// <param name="render">Markdown to HTML service.</param>
    /// <param name="runtime">WebView2 runtime detection.</param>
    /// <param name="paths">Application folders.</param>
    /// <param name="localization">Observable localization service.</param>
    /// <param name="feedback">Snackbar feedback.</param>
    /// <param name="logger">Logger.</param>
    public PreviewViewModel(
        EditorViewModel editor,
        IMarkdownRenderService render,
        IWebViewRuntimeService runtime,
        IAppPaths paths,
        ILocalizationService localization,
        IFeedbackService feedback,
        ILogger<PreviewViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(paths);

        _editor = editor;
        _render = render;
        _feedback = feedback;
        _logger = logger;
        Loc = localization;
        IsAvailable = runtime.IsAvailable();
        UserDataFolder = paths.WebViewDataFolder;

        _editor.TextChanged += OnEditorTextChanged;
        _editor.PropertyChanged += OnEditorPropertyChanged;
        _editor.HeadingSelected += OnHeadingSelected;
    }

    /// <summary>Gets the localized strings.</summary>
    public ILocalizationService Loc { get; }

    /// <summary>Gets a value indicating whether the WebView2 runtime is installed.</summary>
    public bool IsAvailable { get; }

    /// <summary>Gets the WebView2 user data folder.</summary>
    public string UserDataFolder { get; }

    /// <summary>Gets the folder of the current document, used to resolve relative images; null when untitled.</summary>
    public string? DocumentFolder => _editor.FilePath is null ? null : Path.GetDirectoryName(_editor.FilePath);

    [ObservableProperty]
    private string _html = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>Gets or sets a value indicating whether the preview pane is shown (rendering is skipped otherwise).</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>Gets or sets the description of a WebView2 failure reported by the view; null when healthy.</summary>
    [ObservableProperty]
    private string? _webViewError;

    /// <summary>Gets the document index of the heading the preview should scroll to.</summary>
    [ObservableProperty]
    private int _headingScrollIndex = -1;

    /// <summary>Gets a value that changes on every outline click, including a second click on the same heading.</summary>
    [ObservableProperty]
    private int _headingScrollVersion;

    /// <summary>Renders the current text after the debounce delay and publishes the HTML.</summary>
    /// <param name="cancellationToken">Cancellation token; cancelled when a newer edit arrives.</param>
    /// <returns>A task completing once the HTML is published or the update is skipped.</returns>
    public async Task UpdateAsync(CancellationToken cancellationToken)
    {
        if (!IsAvailable || !IsActive)
        {
            return;
        }

        await Task.Delay(AppConfig.PreviewDebounceMs, cancellationToken);
        var result = await _render.RenderHtmlAsync(_editor.Text, cancellationToken);
        if (result.Ok)
        {
            _lastErrorMessage = null;
            Html = result.Data!;
            return;
        }

        var error = result.Error!;
        if (!string.Equals(error.Message, _lastErrorMessage, StringComparison.Ordinal))
        {
            _lastErrorMessage = error.Message;
            _feedback.Show(error);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _editor.TextChanged -= OnEditorTextChanged;
        _editor.PropertyChanged -= OnEditorPropertyChanged;
        _editor.HeadingSelected -= OnHeadingSelected;
        _updateCts?.Cancel();
        _updateCts?.Dispose();
    }

    partial void OnIsActiveChanged(bool value)
    {
        if (value)
        {
            ScheduleUpdate();
        }
    }

    partial void OnWebViewErrorChanged(string? value)
    {
        if (value is null)
        {
            return;
        }

        _logger.LogError("Echec de WebView2: {Detail}", value);
        _feedback.Show(new ResultError(FeedbackType.Danger, Strings.Error_PreviewFailed));
    }

    private void OnHeadingSelected(object? sender, int index)
    {
        if (!IsActive || index < 0)
        {
            return;
        }

        HeadingScrollIndex = index;
        HeadingScrollVersion++;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e) => ScheduleUpdate();

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.FilePath))
        {
            OnPropertyChanged(nameof(DocumentFolder));
        }
    }

    private void ScheduleUpdate()
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
            await UpdateAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer edit superseded this update.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Echec de la mise a jour de l'apercu");
            _feedback.Show(new ResultError(FeedbackType.Danger, Strings.Error_PreviewFailed));
        }
    }
}
