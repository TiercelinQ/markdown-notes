namespace MarkdownNotes.Models;

/// <summary>A heading of a Markdown document and the headings nested under it.</summary>
internal sealed class HeadingNode
{
    private readonly List<HeadingNode> _children = [];

    /// <summary>Initializes a new instance of the <see cref="HeadingNode"/> class.</summary>
    /// <param name="text">Plain text of the heading.</param>
    /// <param name="level">Heading level, 1 to 6.</param>
    /// <param name="offset">Index of the heading in the source text.</param>
    /// <param name="length">Length of the heading in the source text.</param>
    /// <param name="index">Zero-based position of this heading among every heading of the document, in source order.</param>
    public HeadingNode(string text, int level, int offset, int length, int index = 0)
    {
        Text = text;
        Level = level;
        Offset = offset;
        Length = length;
        Index = index;
    }

    /// <summary>Gets the plain text of the heading.</summary>
    public string Text { get; }

    /// <summary>Gets the heading level, 1 to 6.</summary>
    public int Level { get; }

    /// <summary>Gets the index of the heading in the source text.</summary>
    public int Offset { get; }

    /// <summary>Gets the length of the heading in the source text.</summary>
    public int Length { get; }

    /// <summary>Gets the zero-based position of this heading among every heading of the document.</summary>
    public int Index { get; }

    /// <summary>Gets the headings nested under this one.</summary>
    public IReadOnlyList<HeadingNode> Children => _children;

    /// <summary>Adds a nested heading.</summary>
    /// <param name="child">The heading to nest.</param>
    internal void AddChild(HeadingNode child) => _children.Add(child);
}
