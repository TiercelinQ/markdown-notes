using System.Windows.Controls;

namespace MarkdownNotes.Views;

/// <summary>The Markdown source editor.</summary>
internal sealed partial class EditorView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="EditorView"/> class.</summary>
    public EditorView() => InitializeComponent();

    /// <summary>Gives the keyboard focus to the text box.</summary>
    public void FocusEditor() => EditorBox.Focus();
}
