using MarkdownNotes.Helpers;
using Xunit;

namespace MarkdownNotes.Tests.Helpers;

public sealed class LineColumnTests
{
    [Theory]
    [InlineData("", 0, 1, 1)]
    [InlineData("abc", 0, 1, 1)]
    [InlineData("abc", 2, 1, 3)]
    [InlineData("abc", 3, 1, 4)]
    [InlineData("a\nbc", 2, 2, 1)]
    [InlineData("a\nbc", 3, 2, 2)]
    [InlineData("a\n\nb", 3, 3, 1)]
    public void Calcule_la_ligne_et_la_colonne_depuis_un_index(string text, int index, int line, int column)
    {
        Assert.Equal((line, column), LineColumn.FromIndex(text, index));
    }

    [Fact]
    public void Ramene_un_index_negatif_au_debut()
    {
        Assert.Equal((1, 1), LineColumn.FromIndex("abc", -5));
    }

    [Fact]
    public void Ramene_un_index_trop_grand_a_la_fin()
    {
        Assert.Equal((2, 3), LineColumn.FromIndex("a\nbc", 99));
    }

    [Fact]
    public void Refuse_un_texte_null()
    {
        Assert.Throws<ArgumentNullException>(() => LineColumn.FromIndex(null!, 0));
    }
}
