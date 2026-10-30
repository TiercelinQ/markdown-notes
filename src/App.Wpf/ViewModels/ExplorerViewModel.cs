using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownNotes.Services;

namespace MarkdownNotes.ViewModels;

/// <summary>The folder explorer pane: an opened folder shown as a lazily loaded tree of Markdown files.</summary>
internal sealed partial class ExplorerViewModel(
    IFolderService folders,
    IFeedbackService feedback,
    ILocalizationService localization) : ObservableObject
{
    /// <summary>Raised with the file path when the user selects a file in the tree.</summary>
    public event EventHandler<string>? FileActivated;

    /// <summary>Gets the localized strings.</summary>
    public ILocalizationService Loc { get; } = localization;

    /// <summary>Gets the top-level nodes of the opened folder.</summary>
    public ObservableCollection<FolderNodeViewModel> Roots { get; } = [];

    /// <summary>Gets a value indicating whether a folder is open.</summary>
    public bool HasFolder => RootPath is not null;

    /// <summary>Gets or sets a value indicating whether the folder tree is open. The title stays visible when it is closed.</summary>
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFolder))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private string? _rootPath;

    /// <summary>Reads a folder and shows it as the tree root.</summary>
    /// <param name="path">Absolute folder path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the folder was read and is now shown.</returns>
    public async Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken)
    {
        var result = await folders.ListChildrenAsync(path, path, cancellationToken);
        if (!result.Ok)
        {
            feedback.Show(result.Error!);
            return false;
        }

        Roots.Clear();
        foreach (var node in result.Data!)
        {
            Roots.Add(new FolderNodeViewModel(node, this));
        }

        RootPath = path;
        return true;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ExpandNodeAsync(FolderNodeViewModel? node, CancellationToken cancellationToken)
    {
        if (node is null || RootPath is null)
        {
            return;
        }

        var result = await folders.ListChildrenAsync(node.FullPath, RootPath, cancellationToken);
        if (!result.Ok)
        {
            feedback.Show(result.Error!);
            node.SetChildren([]);
            return;
        }

        node.SetChildren(result.Data!.Select(child => new FolderNodeViewModel(child, this)));
    }

    [RelayCommand]
    private void ActivateNode(FolderNodeViewModel? node)
    {
        if (node is { IsDirectory: false })
        {
            FileActivated?.Invoke(this, node.FullPath);
        }
    }

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand(CanExecute = nameof(HasFolder))]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (RootPath is { } path)
        {
            await OpenFolderAsync(path, cancellationToken);
        }
    }
}
