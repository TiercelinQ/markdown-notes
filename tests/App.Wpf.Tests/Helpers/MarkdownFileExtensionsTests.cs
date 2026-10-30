using MarkdownNotes.Helpers;
using Xunit;

namespace MarkdownNotes.Tests.Helpers;

public sealed class MarkdownFileExtensionsTests
{
    [Theory]
    [InlineData("notes.md")]
    [InlineData("NOTES.MD")]
    [InlineData(@"C:\docs\readme.markdown")]
    [InlineData("todo.txt")]
    public void Reconnait_les_extensions_markdown(string path)
    {
        Assert.True(MarkdownFileExtensions.IsMarkdown(path));
    }

    [Theory]
    [InlineData("report.docx")]
    [InlineData("program.exe")]
    [InlineData("sans-extension")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Refuse_les_autres_chemins(string? path)
    {
        Assert.False(MarkdownFileExtensions.IsMarkdown(path));
    }
}
