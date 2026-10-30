using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownNotes.Helpers;
using MarkdownNotes.Models;
using MarkdownNotes.Services;

namespace MarkdownNotes.ViewModels;

/// <summary>State of the document being edited: text, file, selection, dirty flag and pending format edits.</summary>
internal sealed partial class EditorViewModel(IMarkdownFormattingService formatting) : ObservableObject
{
    private string _savedSnapshot = string.Empty;

    /// <summary>Raised after the text changed, whatever the cause (typing, load, edit).</summary>
    public event EventHandler? TextChanged;

    /// <summary>Raised when the outline asks to show a heading. The argument is its document index.</summary>
    public event EventHandler<int>? HeadingSelected;

    /// <summary>Gets the encoding the document is written back with.</summary>
    public Encoding Encoding { get; private set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Gets the file name without its folder, or null for an untitled document.</summary>
    public string? FileName => FilePath is null ? null : Path.GetFileName(FilePath);

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private int _selectionStart;

    [ObservableProperty]
    private int _selectionLength;

    [ObservableProperty]
    private int _line = 1;

    [ObservableProperty]
    private int _column = 1;

    /// <summary>Gets or sets the edit waiting to be applied by the view; the view clears it once applied.</summary>
    [ObservableProperty]
    private TextEdit? _pendingEdit;

    /// <summary>Replaces the document with a freshly read file.</summary>
    /// <param name="document">The document read from disk.</param>
    public void Load(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Encoding = document.Encoding;
        _savedSnapshot = document.Text;
        FilePath = document.Path;
        SelectionStart = 0;
        SelectionLength = 0;
        Text = document.Text;
        IsDirty = false;
    }

    /// <summary>Starts an empty untitled document.</summary>
    public void Reset()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        _savedSnapshot = string.Empty;
        FilePath = null;
        SelectionStart = 0;
        SelectionLength = 0;
        Text = string.Empty;
        IsDirty = false;
    }

    /// <summary>Records a successful save.</summary>
    /// <param name="path">Path the text was written to.</param>
    /// <param name="snapshot">The exact text that was written (typing may have continued during the write).</param>
    public void MarkSaved(string path, string snapshot)
    {
        _savedSnapshot = snapshot;
        FilePath = path;
        IsDirty = !string.Equals(Text, _savedSnapshot, StringComparison.Ordinal);
    }

    /// <summary>Queues an edit for the editor view.</summary>
    /// <param name="edit">The edit to apply.</param>
    public void Apply(TextEdit edit) => PendingEdit = edit;

    /// <summary>Selects a heading in the editor and asks the preview to scroll to the same heading.</summary>
    /// <param name="headingIndex">Zero-based position of the heading among every heading of the document.</param>
    /// <param name="edit">Selection of the heading in the source.</param>
    public void NavigateToHeading(int headingIndex, TextEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        Apply(edit);
        HeadingSelected?.Invoke(this, headingIndex);
    }

    partial void OnTextChanged(string value)
    {
        IsDirty = !string.Equals(value, _savedSnapshot, StringComparison.Ordinal);
        UpdateCaretPosition();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectionStartChanged(int value) => UpdateCaretPosition();

    private void UpdateCaretPosition()
    {
        (Line, Column) = LineColumn.FromIndex(Text, SelectionStart);
    }

    [RelayCommand]
    private void Format(FormatKind kind) => Apply(formatting.Apply(kind, Text, SelectionStart, SelectionLength));
}
