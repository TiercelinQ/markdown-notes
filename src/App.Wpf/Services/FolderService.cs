using System.IO;
using MarkdownNotes.Helpers;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.Services;

/// <summary>Lists the Markdown files and sub-folders of a folder, confined to the opened root.</summary>
internal interface IFolderService
{
    /// <summary>Lists the direct children of a folder: sub-folders first, then Markdown files.</summary>
    /// <param name="folderPath">Folder to list; must be the root or lie under it.</param>
    /// <param name="rootPath">Root folder opened by the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The children, or a warning/danger when the path is outside the root or unreadable.</returns>
    Task<Result<IReadOnlyList<FolderNode>>> ListChildrenAsync(string folderPath, string rootPath, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class FolderService(ILogger<FolderService> logger) : IFolderService
{
    private const FileAttributes Skipped = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FolderNode>>> ListChildrenAsync(
        string folderPath,
        string rootPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var nodes = await Task.Run(() => List(folderPath, rootPath, cancellationToken), cancellationToken).ConfigureAwait(false);
            return Result<IReadOnlyList<FolderNode>>.Success(nodes);
        }
        catch (InvalidDocumentPathException ex)
        {
            logger.LogWarning(ex, "Dossier hors de la racine ouverte");
            return Result<IReadOnlyList<FolderNode>>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_InvalidPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Echec de lecture du dossier");
            return Result<IReadOnlyList<FolderNode>>.Failure(new ResultError(FeedbackType.Danger, Strings.Error_FolderReadFailed));
        }
    }

    private static List<FolderNode> List(string folderPath, string rootPath, CancellationToken cancellationToken)
    {
        var root = Normalize(rootPath);
        var folder = Normalize(folderPath);
        if (!IsUnder(folder, root))
        {
            throw new InvalidDocumentPathException("Le dossier est hors de la racine ouverte.");
        }

        var info = new DirectoryInfo(folder);
        var nodes = new List<FolderNode>();

        foreach (var directory in info.EnumerateDirectories().OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((directory.Attributes & Skipped) == 0)
            {
                nodes.Add(new FolderNode(directory.Name, directory.FullName, IsDirectory: true));
            }
        }

        foreach (var file in info.EnumerateFiles().OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((file.Attributes & Skipped) == 0 && MarkdownFileExtensions.IsMarkdown(file.Name))
            {
                nodes.Add(new FolderNode(file.Name, file.FullName, IsDirectory: false));
            }
        }

        return nodes;
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDocumentPathException("Chemin vide.");
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDocumentPathException("Chemin mal forme: " + ex.Message);
        }
    }

    private static bool IsUnder(string folder, string root)
    {
        if (string.Equals(folder, root, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
