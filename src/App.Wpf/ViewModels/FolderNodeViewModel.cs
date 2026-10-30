using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownNotes.Models;

namespace MarkdownNotes.ViewModels;

/// <summary>A lazily loaded node of the folder tree.</summary>
internal sealed partial class FolderNodeViewModel : ObservableObject
{
    private readonly ExplorerViewModel? _explorer;

    /// <summary>Initializes a new instance of the <see cref="FolderNodeViewModel"/> class.</summary>
    /// <param name="node">The folder or file this node stands for.</param>
    /// <param name="explorer">The owning explorer, notified when the node is expanded or selected.</param>
    public FolderNodeViewModel(FolderNode node, ExplorerViewModel explorer)
    {
        ArgumentNullException.ThrowIfNull(node);

        _explorer = explorer;
        Name = node.Name;
        FullPath = node.FullPath;
        IsDirectory = node.IsDirectory;
        if (IsDirectory)
        {
            Children.Add(new FolderNodeViewModel());
        }
    }

    private FolderNodeViewModel()
    {
        Name = string.Empty;
        FullPath = string.Empty;
        IsPlaceholder = true;
    }

    /// <summary>Gets the display name.</summary>
    public string Name { get; }

    /// <summary>Gets the absolute path.</summary>
    public string FullPath { get; }

    /// <summary>Gets a value indicating whether the node is a folder.</summary>
    public bool IsDirectory { get; }

    /// <summary>Gets a value indicating whether the node only exists to show an expander arrow before the folder is read.</summary>
    public bool IsPlaceholder { get; }

    /// <summary>Gets a value indicating whether the real children were read from disk.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Gets the children; a single placeholder until the folder is expanded.</summary>
    public ObservableCollection<FolderNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Replaces the placeholder with the children read from disk.</summary>
    /// <param name="children">The real children.</param>
    public void SetChildren(IEnumerable<FolderNodeViewModel> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        Children.Clear();
        foreach (var child in children)
        {
            Children.Add(child);
        }

        IsLoaded = true;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && IsDirectory && !IsLoaded && _explorer is not null)
        {
            _explorer.ExpandNodeCommand.Execute(this);
        }
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value && !IsDirectory && !IsPlaceholder && _explorer is not null)
        {
            _explorer.ActivateNodeCommand.Execute(this);
        }
    }
}
