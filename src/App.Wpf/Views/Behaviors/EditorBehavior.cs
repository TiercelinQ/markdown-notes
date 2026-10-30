using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkdownNotes.Models;

namespace MarkdownNotes.Views.Behaviors;

/// <summary>
/// Connects a <see cref="TextBox"/> to the editor view model: reports the selection, and applies a
/// <see cref="TextEdit"/> through <c>SelectedText</c> so the native undo and redo stack stays intact.
/// </summary>
internal static class EditorBehavior
{
    /// <summary>Hooks the selection event; set to true once in XAML.</summary>
    public static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
        "IsAttached", typeof(bool), typeof(EditorBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

    /// <summary>Selection start reported to the view model; bind one way to source.</summary>
    public static readonly DependencyProperty SelectionStartProperty = DependencyProperty.RegisterAttached(
        "SelectionStart", typeof(int), typeof(EditorBehavior), new PropertyMetadata(0));

    /// <summary>Selection length reported to the view model; bind one way to source.</summary>
    public static readonly DependencyProperty SelectionLengthProperty = DependencyProperty.RegisterAttached(
        "SelectionLength", typeof(int), typeof(EditorBehavior), new PropertyMetadata(0));

    /// <summary>Edit queued by the view model; the behavior applies it then clears it.</summary>
    public static readonly DependencyProperty PendingEditProperty = DependencyProperty.RegisterAttached(
        "PendingEdit", typeof(TextEdit), typeof(EditorBehavior), new PropertyMetadata(null, OnPendingEditChanged));

    /// <summary>Gets <see cref="IsAttachedProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <returns>The value.</returns>
    public static bool GetIsAttached(DependencyObject element) => (bool)element.GetValue(IsAttachedProperty);

    /// <summary>Sets <see cref="IsAttachedProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <param name="value">The value.</param>
    public static void SetIsAttached(DependencyObject element, bool value) => element.SetValue(IsAttachedProperty, value);

    /// <summary>Gets <see cref="SelectionStartProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <returns>The value.</returns>
    public static int GetSelectionStart(DependencyObject element) => (int)element.GetValue(SelectionStartProperty);

    /// <summary>Sets <see cref="SelectionStartProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <param name="value">The value.</param>
    public static void SetSelectionStart(DependencyObject element, int value) => element.SetValue(SelectionStartProperty, value);

    /// <summary>Gets <see cref="SelectionLengthProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <returns>The value.</returns>
    public static int GetSelectionLength(DependencyObject element) => (int)element.GetValue(SelectionLengthProperty);

    /// <summary>Sets <see cref="SelectionLengthProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <param name="value">The value.</param>
    public static void SetSelectionLength(DependencyObject element, int value) => element.SetValue(SelectionLengthProperty, value);

    /// <summary>Gets <see cref="PendingEditProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <returns>The value.</returns>
    public static TextEdit? GetPendingEdit(DependencyObject element) => (TextEdit?)element.GetValue(PendingEditProperty);

    /// <summary>Sets <see cref="PendingEditProperty"/>.</summary>
    /// <param name="element">The text box.</param>
    /// <param name="value">The value.</param>
    public static void SetPendingEdit(DependencyObject element, TextEdit? value) => element.SetValue(PendingEditProperty, value);

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox && e.NewValue is true)
        {
            textBox.SelectionChanged += OnSelectionChanged;
        }
    }

    private static void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        textBox.SetCurrentValue(SelectionStartProperty, textBox.SelectionStart);
        textBox.SetCurrentValue(SelectionLengthProperty, textBox.SelectionLength);
    }

    private static void OnPendingEditChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox || e.NewValue is not TextEdit edit)
        {
            return;
        }

        Apply(textBox, edit);
        textBox.SetCurrentValue(PendingEditProperty, null);
    }

    private static void Apply(TextBox textBox, TextEdit edit)
    {
        var length = textBox.Text.Length;
        if (edit.Replacement is not null)
        {
            var start = Math.Clamp(edit.Start, 0, length);
            textBox.Select(start, Math.Clamp(edit.Length, 0, length - start));
            textBox.SelectedText = edit.Replacement;
            length = textBox.Text.Length;
        }

        var selectionStart = Math.Clamp(edit.SelectionStart, 0, length);
        textBox.Select(selectionStart, Math.Clamp(edit.SelectionLength, 0, length - selectionStart));
        Reveal(textBox, selectionStart);
    }

    // A collapsed editor (preview-only) has no viewport yet: the selection is kept for when it is shown.
    private static void Reveal(TextBox textBox, int index)
    {
        if (!textBox.IsVisible)
        {
            return;
        }

        textBox.Focus();
        textBox.UpdateLayout();
        var line = textBox.GetLineIndexFromCharacterIndex(index);
        if (line >= 0)
        {
            textBox.ScrollToLine(line);
        }

        var rect = textBox.GetRectFromCharacterIndex(index);
        if (rect.IsEmpty || FindScrollViewer(textBox) is not { } scrollViewer)
        {
            return;
        }

        var top = rect.Top + scrollViewer.VerticalOffset;
        var bottom = rect.Bottom + scrollViewer.VerticalOffset;
        if (top < scrollViewer.VerticalOffset)
        {
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, top));
        }
        else if (bottom > scrollViewer.VerticalOffset + scrollViewer.ViewportHeight)
        {
            scrollViewer.ScrollToVerticalOffset(bottom - scrollViewer.ViewportHeight);
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer)
            {
                return viewer;
            }

            var nested = FindScrollViewer(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
