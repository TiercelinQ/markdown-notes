using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using MarkdownNotes.Views;
using MarkdownNotes.Views.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Wpf.Ui;

namespace MarkdownNotes;

/// <summary>Composition root: logging, global error handlers, single instance, host and main window.</summary>
internal sealed partial class App : Application
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private IHost? _host;
    private ISingleInstanceService? _singleInstance;

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    public App()
    {
        ConfigureBootstrapLogger();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var startPath = e.Args.Length > 0 ? Path.GetFullPath(e.Args[0]) : null;
            _host = BuildHost();
            _singleInstance = _host.Services.GetRequiredService<ISingleInstanceService>();

            if (!_singleInstance.TryAcquire())
            {
                await _singleInstance.ForwardAsync(startPath, CancellationToken.None);
                Shutdown();
                return;
            }

            _singleInstance.PathReceived += OnPathReceived;
            _singleInstance.StartListening();
            await _host.StartAsync();
            await ShowMainWindowAsync(_host.Services, startPath);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Echec du demarrage de l'application");

            // The host or the main window does not exist: the snackbar cannot be shown, so a native box is the only surface.
            MessageBox.Show(ex.Message, AppConfig.AppDisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _singleInstance?.Dispose();
            if (_host is not null)
            {
                // Off the UI thread so the shutdown wait can never deadlock on the dispatcher.
                Task.Run(() => _host.StopAsync(TimeSpan.FromSeconds(2))).GetAwaiter().GetResult();
                _host.Dispose();
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            Log.Warning(ex, "Arret du host interrompu");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        base.OnExit(e);
    }

    private static string LogFilePath() => Path.Combine(new AppPaths().LogsDirectory, "app-.log");

    private static bool DebugEnabled() => Environment.GetEnvironmentVariable(AppConfig.AppName.ToUpperInvariant() + "_DEBUG") == "1";

    private static LogEventLevel MinimumLevel() =>
        DebugEnabled() ? LogEventLevel.Debug : Enum.Parse<LogEventLevel>(AppConfig.LogLevel);

    private static void ConfigureBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(MinimumLevel())
            .WriteTo.File(
                LogFilePath(),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: AppConfig.LogMaxBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7,
                shared: true,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }

    private static IHost BuildHost() =>
        Host.CreateDefaultBuilder()
            .UseSerilog((_, configuration) =>
            {
                configuration
                    .MinimumLevel.Is(MinimumLevel())
                    .WriteTo.File(
                        LogFilePath(),
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: AppConfig.LogMaxBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 7,
                        shared: true,
                        formatProvider: CultureInfo.InvariantCulture,
                        outputTemplate: OutputTemplate);

                if (DebugEnabled())
                {
                    configuration.WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture);
                }
            })
            .ConfigureServices(RegisterServices)
            .Build();

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddSingleton<IFeedbackService, FeedbackService>();
        services.AddSingleton<IAppThemeService, AppThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<IFolderService, FolderService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();
        services.AddSingleton<IOutlineService, OutlineService>();
        services.AddSingleton<IMarkdownFormattingService, MarkdownFormattingService>();
        services.AddSingleton<ITextSearchService, TextSearchService>();
        services.AddSingleton<IWebViewRuntimeService, WebViewRuntimeService>();
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IUnsavedChangesPrompt, UnsavedChangesPrompt>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();

        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<PreviewViewModel>();
        services.AddSingleton<ExplorerViewModel>();
        services.AddSingleton<OutlineViewModel>();
        services.AddSingleton<FindReplaceViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private async Task ShowMainWindowAsync(IServiceProvider services, string? startPath)
    {
        var preferences = services.GetRequiredService<IPreferencesService>();
        var loaded = await preferences.LoadAsync(CancellationToken.None);

        var current = preferences.Current;
        services.GetRequiredService<ILocalizationService>().SetCulture(current.Culture);
        var themes = services.GetRequiredService<IAppThemeService>();
        themes.Apply(themes.ResolveInitial(current.Theme));

        var shell = services.GetRequiredService<ShellViewModel>();
        shell.StartupPath = startPath;
        shell.StartupError = loaded.Ok ? null : loaded.Error;

        var window = services.GetRequiredService<MainWindow>();
        RestorePlacement(window, current);
        MainWindow = window;

        // Shown once, fully built: no white flash.
        window.Show();
        window.Activate();
    }

    private static void RestorePlacement(Window window, UserPreferences preferences)
    {
        if (preferences.WindowWidth is { } width && preferences.WindowHeight is { } height)
        {
            window.Width = Math.Max(width, AppConfig.WindowMinWidth);
            window.Height = Math.Max(height, AppConfig.WindowMinHeight);
        }

        if (preferences.WindowLeft is { } left && preferences.WindowTop is { } top && IsOnScreen(left, top, window.Width))
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = left;
            window.Top = top;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (preferences.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    // The saved position must leave a usable part of the window on a connected screen.
    private static bool IsOnScreen(double left, double top, double width) =>
        left + (width / 2) > SystemParameters.VirtualScreenLeft
        && left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - (width / 2)
        && top >= SystemParameters.VirtualScreenTop
        && top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - AppConfig.WindowMinHeight / 2;

    private void OnPathReceived(object? sender, PathReceivedEventArgs e) =>
        Dispatcher.InvokeAsync(() => HandleForwardedPath(e.Path));

    private void HandleForwardedPath(string path)
    {
        if (MainWindow is not { } window)
        {
            return;
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        if (!string.IsNullOrWhiteSpace(path) && _host is not null)
        {
            _host.Services.GetRequiredService<ShellViewModel>().OpenPathCommand.Execute(path);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Exception non geree sur le thread UI");
        _host?.Services.GetService<IFeedbackService>()?.Show(
            new ResultError(FeedbackType.Danger, Strings.Error_Unexpected, e.Exception.Message));
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.ExceptionObject as Exception, "Exception non geree sur un thread d'arriere-plan");
        Log.CloseAndFlush();
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Exception de tache non observee");
        e.SetObserved();
    }
}
