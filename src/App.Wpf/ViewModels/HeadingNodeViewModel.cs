using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownNotes.Models;

namespace MarkdownNotes.ViewModels;

/// <summary>A heading of the outline tree; selecting it moves the editor to the heading.</summary>
internal sealed partial class HeadingNodeViewModel : ObservableObject
{
    private readonly OutlineViewModel _outline;

    /// <summary>Initializes a new instance of the <see cref="HeadingNodeViewModel"/> class.</summary>
    /// <param name="node">The heading this node stands for.</param>
    /// <param name="outline">The owning outline, asked to go to the heading when it is selected.</param>
    public HeadingNodeViewModel(HeadingNode node, OutlineViewModel outline)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(outline);

        _outline = outline;
        Text = node.Text;
        Offset = node.Offset;
        Length = node.Length;
        Index = node.Index;
        Children = [.. node.Children.Select(child => new HeadingNodeViewModel(child, outline))];
    }

    /// <summary>Gets the plain text of the heading.</summary>
    public string Text { get; }

    /// <summary>Gets the index of the heading in the document text.</summary>
    public int Offset { get; }

    /// <summary>Gets the length of the heading in the document text.</summary>
    public int Length { get; }

    /// <summary>Gets the zero-based position of this heading among every heading of the document.</summary>
    public int Index { get; }

    /// <summary>Gets the headings nested under this one.</summary>
    public IReadOnlyList<HeadingNodeViewModel> Children { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (!value)
        {
            return;
        }

        _outline.GoToHeadingCommand.Execute(this);
        _ = ClearSelectionAsync();
    }

    // Selection is only a click: it is cleared right after (once the binding update is over)
    // so that clicking the same heading again, after the caret moved, goes there again.
    private async Task ClearSelectionAsync()
    {
        await Task.Yield();
        IsSelected = false;
    }
}
