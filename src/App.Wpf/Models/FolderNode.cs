namespace MarkdownNotes.Models;

/// <summary>An entry of the folder explorer: a sub-folder or a Markdown file.</summary>
/// <param name="Name">Display name (file or folder name).</param>
/// <param name="FullPath">Absolute path.</param>
/// <param name="IsDirectory">True for a folder, false for a file.</param>
internal sealed record FolderNode(string Name, string FullPath, bool IsDirectory);
