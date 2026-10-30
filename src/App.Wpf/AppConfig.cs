using System.Windows.Media;

namespace MarkdownNotes;

/// <summary>Application constants. Visual values live in <c>Themes/Tokens.xaml</c>, never here.</summary>
internal static class AppConfig
{
    // Application

    /// <summary>Technical name: assembly, <c>%APPDATA%</c> folder, log and environment variable prefix.</summary>
    public const string AppName = "MarkdownNotes";

    /// <summary>Name shown in the window title.</summary>
    public const string AppDisplayName = "Markdown Notes";

    /// <summary>Mirror of the <c>.csproj</c> version.</summary>
    public const string AppVersion = "1.2.0";

    // Accent (design-system.md section 2): the only color literal in C#

    /// <summary>Project accent as hex.</summary>
    public const string AccentColor = "#3B6F9E";

    /// <summary>Gets the parsed accent color fed to the accent manager.</summary>
    public static Color AccentColorValue { get; } = (Color)ColorConverter.ConvertFromString(AccentColor);

    // Preferences

    /// <summary>File name of the preferences file under <c>%APPDATA%\MarkdownNotes</c>.</summary>
    public const string PreferencesFileName = "preferences.json";

    // Internationalization

    /// <summary>Default UI culture.</summary>
    public const string DefaultCulture = "fr";

    /// <summary>Supported UI cultures.</summary>
    public static readonly string[] SupportedCultures = ["fr", "en"];

    // Window

    /// <summary>Minimum window width.</summary>
    public const double WindowMinWidth = 800;

    /// <summary>Minimum window height.</summary>
    public const double WindowMinHeight = 600;

    /// <summary>Default window width.</summary>
    public const double WindowDefaultWidth = 1280;

    /// <summary>Default window height.</summary>
    public const double WindowDefaultHeight = 800;

    // Snackbar position (layout.md section 5, Phase 3 choice)

    /// <summary>Default snackbar anchor.</summary>
    public const string SnackbarPosition = "bottom-right";

    // Logging

    /// <summary>Default log level.</summary>
    public const string LogLevel = "Information";

    /// <summary>Rolling file size cap in bytes (1 MB).</summary>
    public const long LogMaxBytes = 1_000_000;

    // Documents

    /// <summary>Largest document the application loads, in bytes (10 MB).</summary>
    public const long MaxDocumentBytes = 10L * 1024 * 1024;

    /// <summary>Extensions accepted as Markdown documents.</summary>
    public static readonly string[] MarkdownExtensions = [".md", ".markdown", ".txt"];

    /// <summary>Number of recent files kept.</summary>
    public const int RecentFilesMax = 10;

    // Preview

    /// <summary>Delay before the preview is refreshed after a keystroke, in milliseconds.</summary>
    public const int PreviewDebounceMs = 250;

    /// <summary>Largest preview page accepted by WebView2 string navigation (below its 2 MB limit), in characters.</summary>
    public const int PreviewMaxChars = 1_500_000;

    /// <summary>Virtual host name mapped to the folder of the current document (relative images).</summary>
    public const string PreviewVirtualHost = "markdown-notes.local";

    /// <summary>Opening tag of the preview page body, shared by the renderer and the WebView host.</summary>
    public const string PreviewBodyStart = "<body>";

    /// <summary>Closing tag of the preview page body, shared by the renderer and the WebView host.</summary>
    public const string PreviewBodyEnd = "</body>";

    // Single instance

    /// <summary>Name of the named mutex guarding the single instance.</summary>
    public const string SingleInstanceMutexName = @"Local\MarkdownNotes.SingleInstance";

    /// <summary>Longest path accepted from the forwarding pipe, in characters.</summary>
    public const int PipeMaxPathLength = 32_767;
}
