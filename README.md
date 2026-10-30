# Markdown Notes - v1.1.0

## Objective

A Windows desktop app to read and write Markdown files. Edit on one side, see the rendered document on the other.

## Features

- Open a file from the File menu, by drag and drop, from the command line (`MarkdownNotes.exe notes.md`) or from the folder explorer.
- Live preview (GitHub Flavored Markdown) in WebView2, with editor only, preview only and side-by-side modes.
- Format toolbar and Format menu: bold, italic, headings 1 to 3, bullet and numbered lists, link, code, quote.
- Find and replace bar (Ctrl+F, Ctrl+H) with match case, next, previous, replace, replace all.
- Mermaid diagrams in the preview (fenced ```` ```mermaid ```` blocks), rendered offline. A block with a syntax error stays as text.
- Folder explorer (lazy tree of Markdown files) and outline pane (heading tree of the open document). A click on a heading moves the editor to it and scrolls the preview to the same heading. Each pane can be collapsed on its own; the titles stay. The left column can be resized. Both panes hide together from the View menu.
- Links to a heading of the same document (`[text](#heading)`) scroll the preview.
- Save and Save as, with a confirmation dialog for unsaved changes. BOM and text encoding preserved, atomic write.
- Recent files (10), light and dark theme, French and English with immediate switch.
- Window position, theme, language, view mode and explorer visibility persisted in `%APPDATA%\MarkdownNotes\preferences.json`.
- Single instance: a second launch hands its file to the running window.

Not in v1.0: tabs, HTML/PDF export, statistics (deferred); auto-save (rejected on purpose).

Application icon: source PNG in `app_icon/icon.png`; `src/App.Wpf/Assets/app.ico` (16 to 256 px) is generated from it and used for the executable, the taskbar and the title bar. To change the icon, regenerate the `.ico` from the new PNG.

## Technical stack

- OS: Windows 10 1809+ · Runtime: .NET 10 (`net10.0-windows10.0.17763.0`) · C# 14 · WPF · WPF UI (Fluent) · Icons: Segoe Fluent Icons
- Composition: IHost + Microsoft.Extensions.DependencyInjection · MVVM: CommunityToolkit.Mvvm
- Markdown: Markdig (raw HTML disabled) · Preview: Microsoft.Web.WebView2 (Evergreen runtime) · Diagrams: Mermaid 11.17.2 (MIT, embedded, see Third-party notices)
- Logging: Serilog (rolling file under `%APPDATA%\MarkdownNotes\logs`)
- DB: none · i18n: Yes (`.resx`, FR default) · Salesforce CLI: No
- Tests: Yes (xUnit v3, NSubstitute)

NuGet packages and versions are pinned in `Directory.Packages.props` (Central Package Management).

## Architecture

```
markdown-notes/
├── MarkdownNotes.sln · Directory.Packages.props · Directory.Build.props · .editorconfig
├── src/App.Wpf/
│   ├── App.xaml(.cs)          Startup: logging, global exception handlers, host, single instance, theme, shell window
│   ├── AppConfig.cs           Constants (name, version, accent, limits, preview settings)
│   ├── Models/                Result, ResultError, MarkdownDocument, FolderNode, HeadingNode, TextEdit, FormatKind,
│   │                          SearchOptions, SearchMatch, ViewMode, AppTheme, UnsavedChoice, UserPreferences
│   ├── Services/              Business logic, one interface per service
│   ├── ViewModels/            Shell, Editor, Preview, Explorer, FolderNode, Outline, HeadingNode, FindReplace
│   ├── Views/                 MainWindow, EditorView, PreviewView, ExplorerView, OutlineView, FindReplaceBar
│   │   ├── Layout/            AppMenuBar, FormatToolBar, StatusBar, file dialogs, unsaved-changes prompt
│   │   └── Behaviors/         EditorBehavior, FileDropBehavior, WebViewBehavior, PaneColumnBehavior, SplitPaneBehavior
│   ├── Helpers/               LineColumn, MarkdownFileExtensions
│   ├── Resources/             Strings.resx (FR), Strings.en.resx, Strings.Designer.cs
│   ├── Assets/                preview.css, mermaid.min.js, mermaid.LICENSE.txt (embedded), app.ico
│   ├── Themes/                Tokens, Tokens.Light, Tokens.Dark, Styles
│   └── Properties/PublishProfiles/win-x64.pubxml
├── tests/App.Wpf.Tests/       Mirror of Models, Helpers, Services, ViewModels and pane behaviors
├── tools/stop-format.ps1      Claude Code Stop-hook guard (format check on C#/XAML changes)
└── docs/release/CHANGELOG.md
```

## Services and commands

| Command | Service method | Data | View |
| ------- | -------------- | ---- | ---- |
| `Shell.New`, `Shell.Exit` | `IUnsavedChangesPrompt.ConfirmAsync` | `UnsavedChoice` | `AppMenuBar` |
| `Shell.Open` | `IFileDialogService.PickOpenFile`, `IDocumentService.ReadAsync` | `MarkdownDocument` | `AppMenuBar` |
| `Shell.OpenPath` (recent, drop, argument, explorer, second instance) | `IDocumentService.ReadAsync`, `IRecentFilesService.AddAsync` | `MarkdownDocument`, `UserPreferences` | `AppMenuBar`, `ExplorerView`, `MainWindow` |
| `Shell.OpenFolder` | `IFileDialogService.PickFolder`, `IFolderService.ListChildrenAsync` | `FolderNode` | `AppMenuBar` |
| `Shell.Save`, `Shell.SaveAs` | `IDocumentService.WriteAsync`, `IFileDialogService.PickSaveFile` | `MarkdownDocument` | `AppMenuBar`, `FormatToolBar` |
| `Shell.SetTheme` | `IAppThemeService.Apply`, `IPreferencesService.UpdateAsync` | `AppTheme` | `AppMenuBar` |
| `Shell.SetLanguage` | `ILocalizationService.SetCulture`, `IPreferencesService.UpdateAsync` | `UserPreferences` | `AppMenuBar` |
| `Shell.SetViewMode`, `Shell.ToggleExplorer` | `IPreferencesService.UpdateAsync` | `ViewMode` | `AppMenuBar`, `MainWindow` |
| `Shell.ShowFind`, `Shell.ShowReplace` | opens the bar | | `FindReplaceBar` |
| `Editor.Format` | `IMarkdownFormattingService.Apply` | `TextEdit` | `FormatToolBar`, `AppMenuBar` |
| `Preview.UpdateAsync` (debounced 250 ms) | `IMarkdownRenderService.RenderHtmlAsync` | text to HTML | `PreviewView` |
| `Explorer.ExpandNode`, `Explorer.Refresh` | `IFolderService.ListChildrenAsync` | `FolderNode` | `ExplorerView` |
| `Explorer.ActivateNode` | raises `FileActivated` handled by `Shell.OpenPath` | `FolderNode` | `ExplorerView` |
| `Outline.RefreshAsync` (debounced 250 ms, after `Editor.TextChanged`) | `IOutlineService.ExtractAsync` | `HeadingNode` | `OutlineView` |
| `Outline.GoToHeading` | `Editor.NavigateToHeading` (selection in the editor, scroll in the preview) | `HeadingNode`, `TextEdit` | `OutlineView`, `EditorView`, `PreviewView` |
| `Explorer.TogglePane`, `Outline.TogglePane` | pane open or folded, title kept | | `ExplorerView`, `OutlineView` |
| `FindReplace.FindNext`, `FindPrevious`, `Replace`, `ReplaceAll` | `ITextSearchService.*` | `SearchMatch`, `TextEdit` | `FindReplaceBar` |

Undo, redo, cut, copy and paste use the native WPF `ApplicationCommands` on the editor.

## Conventions

- Strict MVVM: views bind to ViewModels only; services never reach the UI. Errors travel as `Result<T>` and surface as snackbars (no `MessageBox` for business errors, no inline banner).
- Every visual value is a resource key from `Themes/Tokens.xaml`; elements use named styles from `Themes/Styles.xaml`. Dark mode swaps the theme dictionary at runtime.
- UI thread never blocked: long work is async with a `CancellationToken`; `async void` appears only in framework event handlers wrapped in `try/catch`.
- Security: paths validated and confined, documents capped at 10 MB, raw HTML disabled in the preview, page scripts blocked by a CSP (`default-src 'none'`; the only script that runs is the embedded Mermaid, injected by the app), remote images blocked by the same CSP, external preview links inert (only links to a heading of the same document work), `preferences.json` written atomically.
- Nullable enabled, warnings are errors, analyzers on, XML doc on public members.

## Prerequisites

- .NET SDK 10.0+ to build. Nothing to install to run a self-contained publish.
- WebView2 Evergreen runtime for the preview (present by default on Windows 11). Without it, the preview is disabled with a warning and the editor still works.

## Installation

```
dotnet restore
dotnet run --project src/App.Wpf     # development
dotnet build                         # compilation (warnings are errors)
dotnet format --verify-no-changes    # format + analyzers
dotnet publish src/App.Wpf -c Release -p:PublishProfile=win-x64   # packaging - project path mandatory, app must be closed
```

The publish step writes `publish/win-x64/MarkdownNotes.exe`, a single self-contained file (about 70-150 MB). Copy it anywhere and run it. The executable is not signed: Windows SmartScreen shows a warning on the first run downloaded from the internet.

Dependency check: `dotnet list package --vulnerable --include-transitive`.

The `.claude/settings.json` Stop hook runs `tools/stop-format.ps1` at the end of each Claude Code turn. Tune or remove it there.

## Tests

```
dotnet test
```

## Third-party notices

- Mermaid 11.17.2 (MIT), embedded as `src/App.Wpf/Assets/mermaid.min.js`, license in `Assets/mermaid.LICENSE.txt`. SHA-256 of the bundle: `581ED7D74BD9048D0E3A91363927D72EF22942D7722546B27F7CC29E35390EB8`. To upgrade, replace both files from the npm `mermaid` package (`dist/mermaid.min.js`), update this hash and run the tests.

## Palette

Custom accent `#3B6F9E` (Steel Blue darkened to reach WCAG AA contrast with white text). Everything else follows the Fluent theme, light and dark, with no brush override.
