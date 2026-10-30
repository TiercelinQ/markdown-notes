using System.Text;

namespace MarkdownNotes.Models;

/// <summary>A Markdown document read from disk.</summary>
/// <param name="Path">Absolute file path, or null for a document never saved.</param>
/// <param name="Text">Full text content.</param>
/// <param name="Encoding">Encoding detected on read, reused on write so a BOM is preserved.</param>
internal sealed record MarkdownDocument(string? Path, string Text, Encoding Encoding);
