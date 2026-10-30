using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;

namespace MarkdownNotes.ViewModels;

/// <summary>Orchestrates files, folders, theme, language, view mode, the status bar and window closing.</summary>
internal sealed partial class ShellViewModel : ObservableObject
{
    private readonly IDocumentService _documents;
    private readonly IFileDialogService _dialogs;
    private readonly IUnsavedChangesPrompt _prompt;
    private readonly IRecentFilesService _recent;
    private readonly IPreferencesService _preferences;
    private readonly IAppThemeService _theme;
    private readonly IFeedbackService _feedback;

    /// <summary>Initializes a new instance of the <see cref="ShellViewModel"/> class.</summary>
    /// <param name="documents">Document read and write service.</param>
    /// <param name="dialogs">File and folder pickers.</param>
    /// <param name="prompt">Unsaved changes dialog.</param>
    /// <param name="recent">Recent files list.</param>
    /// <param name="preferences">User preferences.</param>
    /// <param name="theme">Theme service.</param>
    /// <param name="localization">Observable localization service.</param>
    /// <param name="feedback">Snackbar feedback.</param>
    /// <param name="editor">Editor state.</param>
    /// <param name="preview">Preview state.</param>
    /// <param name="explorer">Explorer state.</param>
    /// <param name="outline">Document outline state.</param>
    /// <param name="findReplace">Find and replace bar state.</param>
    public ShellViewModel(
        IDocumentService documents,
        IFileDialogService dialogs,
        IUnsavedChangesPrompt prompt,
        IRecentFilesService recent,
        IPreferencesService preferences,
        IAppThemeService theme,
        ILocalizationService localization,
        IFeedbackService feedback,
        EditorViewModel editor,
        PreviewViewModel preview,
        ExplorerViewModel explorer,
        OutlineViewModel outline,
        FindReplaceViewModel findReplace)
    {
        _documents = documents;
        _dialogs = dialogs;
        _prompt = prompt;
        _recent = recent;
        _preferences = preferences;
        _theme = theme;
        _feedback = feedback;
        Loc = localization;
        Editor = editor;
        Preview = preview;
        Explorer = explorer;
        Outline = outline;
        FindReplace = findReplace;

        Editor.PropertyChanged += OnEditorPropertyChanged;
        Loc.PropertyChanged += OnLocalizationChanged;
        Explorer.FileActivated += OnExplorerFileActivated;
    }

    /// <summary>Raised when the shell asks the window to close (the window runs the unsaved changes check).</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Gets the localized strings.</summary>
    public ILocalizationService Loc { get; }

    /// <summary>Gets the editor state.</summary>
    public EditorViewModel Editor { get; }

    /// <summary>Gets the preview state.</summary>
    public PreviewViewModel Preview { get; }

    /// <summary>Gets the explorer state.</summary>
    public ExplorerViewModel Explorer { get; }

    /// <summary>Gets the document outline state.</summary>
    public OutlineViewModel Outline { get; }

    /// <summary>Gets the find and replace bar state.</summary>
    public FindReplaceViewModel FindReplace { get; }

    /// <summary>Gets the recently opened files, most recent first.</summary>
    public ObservableCollection<string> RecentFiles { get; } = [];

    /// <summary>Gets a value indicating whether there is at least one recent file.</summary>
    public bool HasRecentFiles => RecentFiles.Count > 0;

    /// <summary>Gets or sets the path to open at startup (command line argument).</summary>
    public string? StartupPath { get; set; }

    /// <summary>Gets or sets an error from loading the preferences, shown once the window is ready.</summary>
    public ResultError? StartupError { get; set; }

    /// <summary>Gets the window title.</summary>
    public string Title =>
        (Editor.IsDirty ? "* " : string.Empty) + (Editor.FileName ?? Strings.Status_Untitled) + " - " + AppConfig.AppDisplayName;

    /// <summary>Gets the status bar message.</summary>
    public string StatusText => StatusMessage ?? Strings.Status_Ready;

    /// <summary>Gets the caret position text.</summary>
    public string CursorText => string.Format(CultureInfo.CurrentCulture, Strings.Status_LineColumn, Editor.Line, Editor.Column);

    /// <summary>Gets the modified marker text, empty when the document is saved.</summary>
    public string ModifiedText => Editor.IsDirty ? Strings.Status_Modified : string.Empty;

    /// <summary>Gets a value indicating whether the editor and preview are side by side.</summary>
    public bool IsSplitMode => ViewMode == ViewMode.Split;

    /// <summary>Gets a value indicating whether only the editor is shown.</summary>
    public bool IsEditorOnlyMode => ViewMode == ViewMode.EditorOnly;

    /// <summary>Gets a value indicating whether only the preview is shown.</summary>
    public bool IsPreviewOnlyMode => ViewMode == ViewMode.PreviewOnly;

    /// <summary>Gets a value indicating whether the light theme is active.</summary>
    public bool IsLightTheme => _theme.Current == AppTheme.Light;

    /// <summary>Gets a value indicating whether the dark theme is active.</summary>
    public bool IsDarkTheme => _theme.Current == AppTheme.Dark;

    /// <summary>Gets a value indicating whether the interface is in French.</summary>
    public bool IsFrench => string.Equals(Loc.CurrentCulture, "fr", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the interface is in English.</summary>
    public bool IsEnglish => string.Equals(Loc.CurrentCulture, "en", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string? _statusMessage;

    [ObservableProperty]
    private ViewMode _viewMode = ViewMode.Split;

    [ObservableProperty]
    private bool _explorerVisible = true;

    /// <summary>Applies the stored preferences, restores the last folder and opens the startup file.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when initialization is done.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var prefs = _preferences.Current;
        ViewMode = prefs.ViewMode;
        ExplorerVisible = prefs.ExplorerVisible;
        Preview.IsDarkTheme = _theme.Current == AppTheme.Dark;
        Preview.IsActive = ViewMode != ViewMode.EditorOnly;
        NotifyAppearanceChanged();

        if (StartupError is not null)
        {
            _feedback.Show(StartupError);
        }

        if (!Preview.IsAvailable)
        {
            _feedback.Show(new ResultError(FeedbackType.Warning, Strings.Warning_WebViewMissing));
        }

        await RefreshRecentFilesAsync(cancellationToken);

        if (prefs.LastFolder is { } folder && Directory.Exists(folder))
        {
            await Explorer.OpenFolderAsync(folder, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(StartupPath))
        {
            await OpenPathCoreAsync(StartupPath, cancellationToken);
        }
    }

    /// <summary>Runs the unsaved changes check and, when closing may proceed, stores the window placement.</summary>
    /// <param name="left">Window left.</param>
    /// <param name="top">Window top.</param>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="maximized">Whether the window is maximized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the window may close.</returns>
    public async Task<bool> TryCloseAsync(double left, double top, double width, double height, bool maximized, CancellationToken cancellationToken)
    {
        if (!await ConfirmDiscardAsync(cancellationToken))
        {
            return false;
        }

        var result = await _preferences.UpdateAsync(
            p => p with
            {
                WindowLeft = left,
                WindowTop = top,
                WindowWidth = width,
                WindowHeight = height,
                WindowMaximized = maximized,
            },
            cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
        }

        return true;
    }

    [RelayCommand]
    private async Task NewAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmDiscardAsync(cancellationToken))
        {
            return;
        }

        Editor.Reset();
        StatusMessage = null;
    }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        var path = _dialogs.PickOpenFile();
        if (path is not null)
        {
            await OpenPathCoreAsync(path, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task OpenPathAsync(string? path, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            await OpenPathCoreAsync(path, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync(CancellationToken cancellationToken)
    {
        var path = _dialogs.PickFolder();
        if (path is null || !await Explorer.OpenFolderAsync(path, cancellationToken))
        {
            return;
        }

        ExplorerVisible = true;
        var result = await _preferences.UpdateAsync(p => p with { LastFolder = path, ExplorerVisible = true }, cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken) => await SaveCoreAsync(forcePicker: false, cancellationToken);

    [RelayCommand]
    private async Task SaveAsAsync(CancellationToken cancellationToken) => await SaveCoreAsync(forcePicker: true, cancellationToken);

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private async Task SetThemeAsync(AppTheme theme, CancellationToken cancellationToken)
    {
        _theme.Apply(theme);
        Preview.IsDarkTheme = theme == AppTheme.Dark;
        NotifyAppearanceChanged();
        await UpdatePreferencesAsync(p => p with { Theme = theme }, cancellationToken);
    }

    [RelayCommand]
    private async Task SetLanguageAsync(string? culture, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        Loc.SetCulture(culture);
        NotifyAppearanceChanged();
        await UpdatePreferencesAsync(p => p with { Culture = Loc.CurrentCulture }, cancellationToken);
    }

    [RelayCommand]
    private async Task SetViewModeAsync(ViewMode mode, CancellationToken cancellationToken)
    {
        ViewMode = mode;
        Preview.IsActive = mode != ViewMode.EditorOnly;
        NotifyAppearanceChanged();
        await UpdatePreferencesAsync(p => p with { ViewMode = mode }, cancellationToken);
    }

    [RelayCommand]
    private async Task ToggleExplorerAsync(CancellationToken cancellationToken)
    {
        ExplorerVisible = !ExplorerVisible;
        var visible = ExplorerVisible;
        await UpdatePreferencesAsync(p => p with { ExplorerVisible = visible }, cancellationToken);
    }

    [RelayCommand]
    private void ShowFind() => FindReplace.Show(replace: false);

    [RelayCommand]
    private void ShowReplace() => FindReplace.Show(replace: true);

    private async Task OpenPathCoreAsync(string path, CancellationToken cancellationToken)
    {
        if (!await ConfirmDiscardAsync(cancellationToken))
        {
            return;
        }

        var result = await _documents.ReadAsync(path, cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
            return;
        }

        var document = result.Data!;
        Editor.Load(document);
        StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.Status_Opened, Editor.FileName);
        await TrackRecentAsync(document.Path!, cancellationToken);
    }

    private async Task<bool> SaveCoreAsync(bool forcePicker, CancellationToken cancellationToken)
    {
        var path = Editor.FilePath;
        if (path is null || forcePicker)
        {
            path = _dialogs.PickSaveFile(Editor.FileName ?? Strings.Status_Untitled + ".md");
            if (path is null)
            {
                return false;
            }
        }

        var text = Editor.Text;
        var result = await _documents.WriteAsync(path, text, Editor.Encoding, cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
            return false;
        }

        Editor.MarkSaved(path, text);
        StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.Status_Saved, Editor.FileName);
        await TrackRecentAsync(path, cancellationToken);
        return true;
    }

    private async Task<bool> ConfirmDiscardAsync(CancellationToken cancellationToken)
    {
        if (!Editor.IsDirty)
        {
            return true;
        }

        var choice = await _prompt.ConfirmAsync(Editor.FileName ?? Strings.Status_Untitled, cancellationToken);
        return choice switch
        {
            UnsavedChoice.Save => await SaveCoreAsync(forcePicker: false, cancellationToken),
            UnsavedChoice.Discard => true,
            _ => false,
        };
    }

    private async Task TrackRecentAsync(string path, CancellationToken cancellationToken)
    {
        var result = await _recent.AddAsync(path, cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
        }

        await RefreshRecentFilesAsync(cancellationToken);
    }

    private async Task RefreshRecentFilesAsync(CancellationToken cancellationToken)
    {
        var files = await _recent.ListAsync(cancellationToken);
        RecentFiles.Clear();
        foreach (var file in files)
        {
            RecentFiles.Add(file);
        }

        OnPropertyChanged(nameof(HasRecentFiles));
    }

    private async Task UpdatePreferencesAsync(Func<UserPreferences, UserPreferences> update, CancellationToken cancellationToken)
    {
        var result = await _preferences.UpdateAsync(update, cancellationToken);
        if (!result.Ok)
        {
            _feedback.Show(result.Error!);
        }
    }

    private void NotifyAppearanceChanged()
    {
        OnPropertyChanged(nameof(IsSplitMode));
        OnPropertyChanged(nameof(IsEditorOnlyMode));
        OnPropertyChanged(nameof(IsPreviewOnlyMode));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsFrench));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CursorText));
        OnPropertyChanged(nameof(ModifiedText));
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(EditorViewModel.FilePath):
            case nameof(EditorViewModel.IsDirty):
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ModifiedText));
                break;
            case nameof(EditorViewModel.Line):
            case nameof(EditorViewModel.Column):
                OnPropertyChanged(nameof(CursorText));
                break;
        }
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e) => NotifyAppearanceChanged();

    private void OnExplorerFileActivated(object? sender, string path) => OpenPathCommand.Execute(path);
}
