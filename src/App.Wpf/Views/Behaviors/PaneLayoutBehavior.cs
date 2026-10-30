using System.Windows;
using System.Windows.Controls;

namespace MarkdownNotes.Views.Behaviors;

/// <summary>
/// Width of the explorer column. A grid splitter can change it while the pane is open;
/// closing the pane remembers that width and restores it.
/// </summary>
internal static class PaneColumnBehavior
{
    /// <summary>Whether the column takes its open width.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen", typeof(bool), typeof(PaneColumnBehavior), new PropertyMetadata(true, OnChanged));

    /// <summary>Width used the first time the column opens, in device-independent pixels.</summary>
    public static readonly DependencyProperty OpenWidthProperty = DependencyProperty.RegisterAttached(
        "OpenWidth", typeof(double), typeof(PaneColumnBehavior), new PropertyMetadata(0d, OnChanged));

    /// <summary>Smallest width the splitter can give the open column.</summary>
    public static readonly DependencyProperty MinOpenWidthProperty = DependencyProperty.RegisterAttached(
        "MinOpenWidth", typeof(double), typeof(PaneColumnBehavior), new PropertyMetadata(0d, OnChanged));

    private static readonly DependencyProperty RememberedWidthProperty = DependencyProperty.RegisterAttached(
        "RememberedWidth", typeof(double), typeof(PaneColumnBehavior), new PropertyMetadata(0d));

    /// <summary>Gets <see cref="IsOpenProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <returns>The value.</returns>
    public static bool GetIsOpen(DependencyObject element) => (bool)element.GetValue(IsOpenProperty);

    /// <summary>Sets <see cref="IsOpenProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <param name="value">The value.</param>
    public static void SetIsOpen(DependencyObject element, bool value) => element.SetValue(IsOpenProperty, value);

    /// <summary>Gets <see cref="OpenWidthProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <returns>The value.</returns>
    public static double GetOpenWidth(DependencyObject element) => (double)element.GetValue(OpenWidthProperty);

    /// <summary>Sets <see cref="OpenWidthProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <param name="value">The value.</param>
    public static void SetOpenWidth(DependencyObject element, double value) => element.SetValue(OpenWidthProperty, value);

    /// <summary>Gets <see cref="MinOpenWidthProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <returns>The value.</returns>
    public static double GetMinOpenWidth(DependencyObject element) => (double)element.GetValue(MinOpenWidthProperty);

    /// <summary>Sets <see cref="MinOpenWidthProperty"/>.</summary>
    /// <param name="element">The column.</param>
    /// <param name="value">The value.</param>
    public static void SetMinOpenWidth(DependencyObject element, double value) => element.SetValue(MinOpenWidthProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColumnDefinition column)
        {
            Apply(column);
        }
    }

    private static void Apply(ColumnDefinition column)
    {
        if (GetIsOpen(column))
        {
            var remembered = (double)column.GetValue(RememberedWidthProperty);
            var openWidth = GetOpenWidth(column);
            if (remembered <= 0 && openWidth <= 0)
            {
                return;
            }

            column.MinWidth = GetMinOpenWidth(column);
            column.MaxWidth = double.PositiveInfinity;
            column.Width = new GridLength(remembered > 0 ? remembered : openWidth);
            return;
        }

        if (column.Width.IsAbsolute && column.Width.Value > 0)
        {
            column.SetValue(RememberedWidthProperty, column.Width.Value);
        }

        column.MinWidth = 0;
        column.MaxWidth = 0;
        column.Width = new GridLength(0);
    }
}

/// <summary>
/// Row heights of the two stacked panes. Closing one gives the other the remaining space
/// and keeps its title row; opening both again restores the split the user dragged.
/// </summary>
internal static class SplitPaneBehavior
{
    /// <summary>Whether the first pane (row 0) is open.</summary>
    public static readonly DependencyProperty FirstOpenProperty = DependencyProperty.RegisterAttached(
        "FirstOpen", typeof(bool), typeof(SplitPaneBehavior), new PropertyMetadata(true, OnChanged));

    /// <summary>Whether the second pane (row 2) is open. Row 1 is the splitter.</summary>
    public static readonly DependencyProperty SecondOpenProperty = DependencyProperty.RegisterAttached(
        "SecondOpen", typeof(bool), typeof(SplitPaneBehavior), new PropertyMetadata(true, OnChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(PaneState), typeof(SplitPaneBehavior), new PropertyMetadata(null));

    /// <summary>Gets <see cref="FirstOpenProperty"/>.</summary>
    /// <param name="element">The grid.</param>
    /// <returns>The value.</returns>
    public static bool GetFirstOpen(DependencyObject element) => (bool)element.GetValue(FirstOpenProperty);

    /// <summary>Sets <see cref="FirstOpenProperty"/>.</summary>
    /// <param name="element">The grid.</param>
    /// <param name="value">The value.</param>
    public static void SetFirstOpen(DependencyObject element, bool value) => element.SetValue(FirstOpenProperty, value);

    /// <summary>Gets <see cref="SecondOpenProperty"/>.</summary>
    /// <param name="element">The grid.</param>
    /// <returns>The value.</returns>
    public static bool GetSecondOpen(DependencyObject element) => (bool)element.GetValue(SecondOpenProperty);

    /// <summary>Sets <see cref="SecondOpenProperty"/>.</summary>
    /// <param name="element">The grid.</param>
    /// <param name="value">The value.</param>
    public static void SetSecondOpen(DependencyObject element, bool value) => element.SetValue(SecondOpenProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid { RowDefinitions.Count: >= 3 } grid)
        {
            return;
        }

        var state = grid.GetValue(StateProperty) as PaneState ?? new PaneState();
        grid.SetValue(StateProperty, state);

        var first = grid.RowDefinitions[0];
        var second = grid.RowDefinitions[2];
        var firstOpen = GetFirstOpen(grid);
        var secondOpen = GetSecondOpen(grid);

        if (state.FirstOpen && state.SecondOpen)
        {
            Remember(first, state, firstPane: true);
            Remember(second, state, firstPane: false);
        }

        state.FirstOpen = firstOpen;
        state.SecondOpen = secondOpen;
        first.Height = HeightFor(firstOpen, secondOpen, state.FirstHeight);
        second.Height = HeightFor(secondOpen, firstOpen, state.SecondHeight);
    }

    private static void Remember(RowDefinition row, PaneState state, bool firstPane)
    {
        if (!row.Height.IsAbsolute || row.Height.Value <= 0)
        {
            return;
        }

        if (firstPane)
        {
            state.FirstHeight = row.Height.Value;
        }
        else
        {
            state.SecondHeight = row.Height.Value;
        }
    }

    private static GridLength HeightFor(bool open, bool otherOpen, double remembered)
    {
        if (!open)
        {
            return GridLength.Auto;
        }

        if (!otherOpen || remembered <= 0)
        {
            return new GridLength(1, GridUnitType.Star);
        }

        return new GridLength(remembered);
    }

    private sealed class PaneState
    {
        public bool FirstOpen { get; set; } = true;

        public bool SecondOpen { get; set; } = true;

        public double FirstHeight { get; set; }

        public double SecondHeight { get; set; }
    }
}
