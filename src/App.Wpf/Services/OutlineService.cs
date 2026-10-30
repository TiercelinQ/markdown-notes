using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownNotes.Models;

namespace MarkdownNotes.Services;

/// <summary>Builds the outline (heading tree) of a Markdown text.</summary>
internal interface IOutlineService
{
    /// <summary>Extracts the headings of a Markdown text as a tree: a heading is nested under the closest previous heading of a lower level.</summary>
    /// <param name="markdown">Markdown source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The top-level headings; empty when the text has none.</returns>
    Task<IReadOnlyList<HeadingNode>> ExtractAsync(string markdown, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class OutlineService : IOutlineService
{
    // Markdig parses the document, so "#" lines inside a code fence are not headings and setext headings are found.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    /// <inheritdoc />
    public Task<IReadOnlyList<HeadingNode>> ExtractAsync(string markdown, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return Task.Run<IReadOnlyList<HeadingNode>>(() => Extract(markdown, cancellationToken), cancellationToken);
    }

    private static List<HeadingNode> Extract(string markdown, CancellationToken cancellationToken)
    {
        var roots = new List<HeadingNode>();
        var open = new Stack<HeadingNode>();
        var index = 0;

        foreach (var heading in Markdown.Parse(markdown, Pipeline).Descendants<HeadingBlock>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The index counts every heading block, including an empty one that is not shown,
            // so it stays aligned with the h1..h6 elements of the preview.
            var text = PlainText(heading);
            var node = new HeadingNode(text, heading.Level, heading.Span.Start, heading.Span.Length, index);
            index++;
            if (text.Length == 0)
            {
                continue;
            }
            while (open.Count > 0 && open.Peek().Level >= node.Level)
            {
                open.Pop();
            }

            if (open.Count == 0)
            {
                roots.Add(node);
            }
            else
            {
                open.Peek().AddChild(node);
            }

            open.Push(node);
        }

        return roots;
    }

    private static string PlainText(HeadingBlock heading)
    {
        if (heading.Inline is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        foreach (var inline in heading.Inline.Descendants())
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
            }
        }

        return text.ToString().Trim();
    }
}
