using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;

namespace MarkdownNotes.ViewModels;

/// <summary>The inline find and replace bar acting on the editor text.</summary>
internal sealed partial class FindReplaceViewModel(
    EditorViewModel editor,
    ITextSearchService search,
    ILocalizationService localization) : ObservableObject
{
    /// <summary>Gets the localized strings.</summary>
    public ILocalizationService Loc { get; } = localization;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isReplaceVisible;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private string _replacement = string.Empty;

    [ObservableProperty]
    private bool _matchCase;

    [ObservableProperty]
    private string _statusText = string.Empty;

    private SearchOptions Options => new(Query, Replacement, MatchCase);

    /// <summary>Opens the bar.</summary>
    /// <param name="replace">True to also show the replacement field.</param>
    public void Show(bool replace)
    {
        IsReplaceVisible = replace;
        IsVisible = true;
        StatusText = string.Empty;
    }

    partial void OnQueryChanged(string value) => StatusText = string.Empty;

    [RelayCommand]
    private void FindNext()
    {
        var match = search.FindNext(editor.Text, Options, editor.SelectionStart + editor.SelectionLength);
        Select(match);
    }

    [RelayCommand]
    private void FindPrevious()
    {
        var match = search.FindPrevious(editor.Text, Options, editor.SelectionStart);
        Select(match);
    }

    [RelayCommand]
    private void Replace()
    {
        var edit = search.ReplaceCurrent(editor.Text, Options, editor.SelectionStart, editor.SelectionLength);
        if (edit is null)
        {
            FindNext();
            return;
        }

        StatusText = string.Empty;
        editor.Apply(edit);
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        var (edit, count) = search.ReplaceAll(editor.Text, Options);
        if (edit is null)
        {
            StatusText = Strings.Find_NoMatch;
            return;
        }

        StatusText = string.Format(CultureInfo.CurrentCulture, Strings.Find_Replaced, count);
        editor.Apply(edit);
    }

    [RelayCommand]
    private void Close() => IsVisible = false;

    private void Select(SearchMatch? match)
    {
        if (match is null)
        {
            StatusText = Strings.Find_NoMatch;
            return;
        }

        StatusText = string.Empty;
        editor.Apply(new TextEdit(match.Start, match.Length, Replacement: null, match.Start, match.Length));
    }
}
