using System.Windows;
using System.Windows.Controls;

namespace MarkdownNotes.Views;

/// <summary>The inline find and replace bar.</summary>
internal sealed partial class FindReplaceBar : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="FindReplaceBar"/> class.</summary>
    public FindReplaceBar() => InitializeComponent();

    private void OnBarVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            QueryBox.Focus();
            QueryBox.SelectAll();
        }
    }
}
