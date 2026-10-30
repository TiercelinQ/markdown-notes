using System.ComponentModel;
using System.Windows;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MarkdownNotes.Views;

/// <summary>The application shell: title bar, menus, toolbar, explorer, editor, preview and status bar.</summary>
internal sealed partial class MainWindow : FluentWindow, IDisposable
{
    private readonly ShellViewModel _shell;
    private readonly IFeedbackService _feedback;
    private readonly ILogger<MainWindow> _logger;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _isClosing;
    private bool _closeConfirmed;

    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    /// <param name="shell">The shell view model.</param>
    /// <param name="snackbar">WPF UI snackbar service; receives the snackbar host.</param>
    /// <param name="dialogs">WPF UI dialog service; receives the dialog host.</param>
    /// <param name="feedback">Snackbar feedback.</param>
    /// <param name="logger">Logger.</param>
    public MainWindow(
        ShellViewModel shell,
        ISnackbarService snackbar,
        IContentDialogService dialogs,
        IFeedbackService feedback,
        ILogger<MainWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(snackbar);
        ArgumentNullException.ThrowIfNull(dialogs);

        _shell = shell;
        _feedback = feedback;
        _logger = logger;
        DataContext = shell;

        InitializeComponent();

        snackbar.SetSnackbarPresenter(SnackbarHost);
        dialogs.SetDialogHost(DialogHost);

        MinWidth = AppConfig.WindowMinWidth;
        MinHeight = AppConfig.WindowMinHeight;
        Width = AppConfig.WindowDefaultWidth;
        Height = AppConfig.WindowDefaultHeight;

        _shell.CloseRequested += OnCloseRequested;
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            EditorPane.FocusEditor();
            await _shell.InitializeAsync(_lifetime.Token);
        }
        catch (OperationCanceledException)
        {
            // The window closed during startup.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Echec de l'initialisation du shell");
            _feedback.Show(new ResultError(FeedbackType.Danger, Strings.Error_Unexpected));
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed)
        {
            return;
        }

        e.Cancel = true;
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        try
        {
            var bounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
            var canClose = await _shell.TryCloseAsync(
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                WindowState == WindowState.Maximized,
                CancellationToken.None);
            if (canClose)
            {
                _closeConfirmed = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Echec de la fermeture, fermeture forcee");
            _closeConfirmed = true;
            Close();
        }
        finally
        {
            _isClosing = false;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _lifetime.Dispose();

    private void OnClosed(object? sender, EventArgs e)
    {
        _lifetime.Cancel();
        Dispose();
    }
}
