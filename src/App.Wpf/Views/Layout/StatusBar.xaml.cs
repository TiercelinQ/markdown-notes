using System.Windows.Controls;

namespace MarkdownNotes.Views.Layout;

/// <summary>File path, status message, modified marker and caret position.</summary>
internal sealed partial class StatusBar : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="StatusBar"/> class.</summary>
    public StatusBar() => InitializeComponent();
}
