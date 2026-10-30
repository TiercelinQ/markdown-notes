using System.Windows.Controls;

namespace MarkdownNotes.Views.Layout;

/// <summary>File, Edit, Format, View and Language menus bound to the shell view model.</summary>
internal sealed partial class AppMenuBar : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="AppMenuBar"/> class.</summary>
    public AppMenuBar() => InitializeComponent();
}
