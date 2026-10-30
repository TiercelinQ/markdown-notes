using System.Windows;
using System.Windows.Input;

namespace MarkdownNotes.Views.Behaviors;

/// <summary>Relays a file dropped on an element to a command, with the first dropped path as parameter.</summary>
internal static class FileDropBehavior
{
    /// <summary>Command run with the dropped file path.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata(null, OnCommandChanged));

    /// <summary>Gets <see cref="CommandProperty"/>.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    /// <summary>Sets <see cref="CommandProperty"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.PreviewDragOver -= OnPreviewDragOver;
        element.PreviewDrop -= OnPreviewDrop;
        if (e.NewValue is not null)
        {
            element.AllowDrop = true;
            element.PreviewDragOver += OnPreviewDragOver;
            element.PreviewDrop += OnPreviewDrop;
        }
    }

    private static void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private static void OnPreviewDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files)
        {
            return;
        }

        e.Handled = true;
        var command = GetCommand((DependencyObject)sender);
        if (command?.CanExecute(files[0]) == true)
        {
            command.Execute(files[0]);
        }
    }
}
