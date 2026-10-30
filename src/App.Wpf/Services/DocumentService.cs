using System.IO;
using System.Text;
using MarkdownNotes.Helpers;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.Services;

/// <summary>Reads and writes Markdown documents on disk.</summary>
internal interface IDocumentService
{
    /// <summary>Reads a document, detecting its encoding.</summary>
    /// <param name="path">Path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document, or a warning/danger when the path is invalid, the file missing, too large or unreadable.</returns>
    Task<Result<MarkdownDocument>> ReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>Writes a document through a temporary file, so a failed write never truncates the target.</summary>
    /// <param name="path">Target path.</param>
    /// <param name="text">Full text.</param>
    /// <param name="encoding">Encoding to write with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a warning/danger when the path is invalid or the write fails.</returns>
    Task<Result<Unit>> WriteAsync(string path, string text, Encoding encoding, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class DocumentService(ILogger<DocumentService> logger) : IDocumentService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public async Task<Result<MarkdownDocument>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var full = ValidatePath(path);
            var info = new FileInfo(full);
            if (!info.Exists)
            {
                return Result<MarkdownDocument>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_FileNotFound));
            }

            if (info.Length > AppConfig.MaxDocumentBytes)
            {
                throw new DocumentTooLargeException($"Fichier de {info.Length} octets.");
            }

            using var reader = new StreamReader(full, Utf8NoBom, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return Result<MarkdownDocument>.Success(new MarkdownDocument(full, text, reader.CurrentEncoding));
        }
        catch (InvalidDocumentPathException ex)
        {
            logger.LogWarning(ex, "Chemin de document refuse");
            return Result<MarkdownDocument>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_InvalidPath));
        }
        catch (DocumentTooLargeException ex)
        {
            logger.LogWarning(ex, "Document trop volumineux");
            return Result<MarkdownDocument>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_FileTooLarge));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Echec de lecture du document");
            return Result<MarkdownDocument>.Failure(new ResultError(FeedbackType.Danger, Strings.Error_ReadFailed));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> WriteAsync(string path, string text, Encoding encoding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(encoding);

        string? temporary = null;
        try
        {
            var full = ValidatePath(path);
            temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(temporary, text, encoding, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, full, overwrite: true);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (InvalidDocumentPathException ex)
        {
            logger.LogWarning(ex, "Chemin de document refuse");
            return Result<Unit>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_InvalidPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Echec d'ecriture du document");
            return Result<Unit>.Failure(new ResultError(FeedbackType.Danger, Strings.Error_WriteFailed));
        }
        finally
        {
            if (temporary is not null && File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDocumentPathException("Chemin vide.");
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDocumentPathException("Chemin mal forme: " + ex.Message);
        }

        if (!MarkdownFileExtensions.IsMarkdown(full))
        {
            throw new InvalidDocumentPathException("Extension non prise en charge.");
        }

        return full;
    }
}
