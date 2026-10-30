using System.Windows.Controls;

namespace MarkdownNotes.Views;

/// <summary>The rendered Markdown preview (WebView2).</summary>
internal sealed partial class PreviewView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="PreviewView"/> class.</summary>
    public PreviewView() => InitializeComponent();
}
