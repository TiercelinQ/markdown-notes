using System.IO;
using MarkdownNotes.Models;

namespace MarkdownNotes.Services;

/// <summary>Keeps the list of the most recently used files.</summary>
internal interface IRecentFilesService
{
    /// <summary>Lists the recent files that still exist, newest first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing recent file paths.</returns>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Moves a file to the top of the list, without duplicates.</summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the preferences write failure.</returns>
    Task<Result<Unit>> AddAsync(string path, CancellationToken cancellationToken);

    /// <summary>Removes a file from the list (for example when it no longer exists).</summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or the preferences write failure.</returns>
    Task<Result<Unit>> RemoveAsync(string path, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class RecentFilesService(IPreferencesService preferences) : IRecentFilesService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken)
    {
        var candidates = preferences.Current.RecentFiles;
        return await Task.Run(() => candidates.Where(File.Exists).ToList(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<Unit>> AddAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return preferences.UpdateAsync(
            current => current with
            {
                RecentFiles = [path, .. current.RecentFiles.Where(other => !SamePath(other, path))],
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<Unit>> RemoveAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return preferences.UpdateAsync(
            current => current with { RecentFiles = current.RecentFiles.Where(other => !SamePath(other, path)).ToList() },
            cancellationToken);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
