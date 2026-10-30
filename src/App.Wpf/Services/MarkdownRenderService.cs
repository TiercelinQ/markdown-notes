using System.IO;
using System.Text;
using Markdig;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;

namespace MarkdownNotes.Services;

/// <summary>Converts Markdown into the HTML page shown by the preview.</summary>
internal interface IMarkdownRenderService
{
    /// <summary>Renders a Markdown text into a complete, self-contained HTML page.</summary>
    /// <param name="markdown">Markdown source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTML page, or a warning when it is too large for the preview.</returns>
    Task<Result<string>> RenderHtmlAsync(string markdown, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class MarkdownRenderService : IMarkdownRenderService
{
    private const string StylesheetResource = "MarkdownNotes.Assets.preview.css";

    // Raw HTML in the source is disabled: the preview never executes author-supplied markup.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Lazy<string> Stylesheet = new(LoadStylesheet);

    /// <inheritdoc />
    public Task<Result<string>> RenderHtmlAsync(string markdown, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return Task.Run(() => Render(markdown, cancellationToken), cancellationToken);
    }

    private static Result<string> Render(string markdown, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var body = Markdown.ToHtml(markdown, Pipeline);
        cancellationToken.ThrowIfCancellationRequested();

        var page = BuildPage(body);
        return page.Length > AppConfig.PreviewMaxChars
            ? Result<string>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_PreviewTooLarge))
            : Result<string>.Success(page);
    }

    private static string BuildPage(string body)
    {
        var host = AppConfig.PreviewVirtualHost;
        return new StringBuilder()
            .Append("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n")
            .Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; img-src https://")
            .Append(host)
            .Append(" data:; style-src 'unsafe-inline'; script-src 'unsafe-eval'\">\n")
            .Append("<base href=\"https://").Append(host).Append("/\">\n")
            .Append("<style>").Append(Stylesheet.Value).Append("</style>\n</head>\n")
            .Append(AppConfig.PreviewBodyStart).Append(body).Append(AppConfig.PreviewBodyEnd)
            .Append("\n</html>")
            .ToString();
    }

    private static string LoadStylesheet()
    {
        using var stream = typeof(MarkdownRenderService).Assembly.GetManifestResourceStream(StylesheetResource)
            ?? throw new InvalidOperationException($"Ressource embarquee introuvable: {StylesheetResource}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
