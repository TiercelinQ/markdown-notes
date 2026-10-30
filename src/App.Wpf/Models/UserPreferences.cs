namespace MarkdownNotes.Models;

/// <summary>Persisted, non-secret user preferences (<c>preferences.json</c>).</summary>
internal sealed record UserPreferences
{
    /// <summary>Gets the chosen theme, or null to follow the Windows app theme.</summary>
    public AppTheme? Theme { get; init; }

    /// <summary>Gets the UI culture code (<c>fr</c> or <c>en</c>).</summary>
    public string Culture { get; init; } = AppConfig.DefaultCulture;

    /// <summary>Gets the visible panes of the editing area.</summary>
    public ViewMode ViewMode { get; init; } = ViewMode.Split;

    /// <summary>Gets a value indicating whether the folder explorer is shown.</summary>
    public bool ExplorerVisible { get; init; } = true;

    /// <summary>Gets the last opened folder, reopened at startup.</summary>
    public string? LastFolder { get; init; }

    /// <summary>Gets the most recently used files, newest first.</summary>
    public IReadOnlyList<string> RecentFiles { get; init; } = [];

    /// <summary>Gets the saved window left edge.</summary>
    public double? WindowLeft { get; init; }

    /// <summary>Gets the saved window top edge.</summary>
    public double? WindowTop { get; init; }

    /// <summary>Gets the saved window width.</summary>
    public double? WindowWidth { get; init; }

    /// <summary>Gets the saved window height.</summary>
    public double? WindowHeight { get; init; }

    /// <summary>Gets a value indicating whether the window was maximized.</summary>
    public bool WindowMaximized { get; init; }

    /// <summary>Gets the snackbar position (<c>bottom-right</c> by default).</summary>
    public string SnackbarPosition { get; init; } = AppConfig.SnackbarPosition;
}
