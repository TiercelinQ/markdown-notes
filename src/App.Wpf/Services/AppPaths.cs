using System.IO;

namespace MarkdownNotes.Services;

/// <summary>Resolves the folders the application writes to, always under <c>%APPDATA%</c>.</summary>
internal interface IAppPaths
{
    /// <summary>Gets the per-user data folder.</summary>
    string UserData { get; }

    /// <summary>Gets the log folder.</summary>
    string LogsDirectory { get; }

    /// <summary>Gets the preferences file path.</summary>
    string PreferencesFile { get; }

    /// <summary>Gets the WebView2 user data folder.</summary>
    string WebViewDataFolder { get; }
}

/// <inheritdoc />
internal sealed class AppPaths : IAppPaths
{
    /// <inheritdoc />
    public string UserData { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppConfig.AppName);

    /// <inheritdoc />
    public string LogsDirectory => Path.Combine(UserData, "logs");

    /// <inheritdoc />
    public string PreferencesFile => Path.Combine(UserData, AppConfig.PreferencesFileName);

    /// <inheritdoc />
    public string WebViewDataFolder => Path.Combine(UserData, "WebView2");
}
