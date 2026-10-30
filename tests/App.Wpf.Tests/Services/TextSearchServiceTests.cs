using MarkdownNotes.Models;
using MarkdownNotes.Services;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class TextSearchServiceTests
{
    private const string Text = "un deux un";

    private readonly TextSearchService _service = new();

    private static SearchOptions Options(string query, string replacement = "", bool matchCase = false) =>
        new(query, replacement, matchCase);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 0)]
    public void Trouve_l_occurrence_suivante_et_reprend_au_debut_en_fin_de_texte(int from, int expectedStart)
    {
        var match = _service.FindNext(Text, Options("un"), from);

        Assert.Equal(new SearchMatch(expectedStart, 2), match);
    }

    [Fact]
    public void Retourne_null_quand_la_requete_est_absente_ou_vide()
    {
        Assert.Null(_service.FindNext(Text, Options("trois"), 0));
        Assert.Null(_service.FindNext(Text, Options(string.Empty), 0));
        Assert.Null(_service.FindPrevious(Text, Options("trois"), 5));
        Assert.Null(_service.FindPrevious(Text, Options(string.Empty), 5));
    }

    [Fact]
    public void Respecte_la_casse_quand_elle_est_demandee()
    {
        var sensitive = _service.FindNext("Un un", Options("un", matchCase: true), 0);
        var insensitive = _service.FindNext("Un un", Options("un"), 0);

        Assert.Equal(new SearchMatch(3, 2), sensitive);
        Assert.Equal(new SearchMatch(0, 2), insensitive);
    }

    [Theory]
    [InlineData(10, 8)]
    [InlineData(8, 0)]
    [InlineData(0, 8)]
    public void Trouve_l_occurrence_precedente_et_reprend_a_la_fin_en_debut_de_texte(int before, int expectedStart)
    {
        var match = _service.FindPrevious(Text, Options("un"), before);

        Assert.Equal(new SearchMatch(expectedStart, 2), match);
    }

    [Fact]
    public void Remplace_l_occurrence_selectionnee_et_selectionne_la_suivante()
    {
        var edit = _service.ReplaceCurrent(Text, Options("un", "1"), 0, 2);

        Assert.Equal(new TextEdit(0, 2, "1", 7, 2), edit);
    }

    [Fact]
    public void Place_le_curseur_apres_le_remplacement_quand_il_n_y_a_plus_d_occurrence()
    {
        var edit = _service.ReplaceCurrent("un", Options("un", "x"), 0, 2);

        Assert.Equal(new TextEdit(0, 2, "x", 1, 0), edit);
    }

    [Fact]
    public void Ne_remplace_pas_quand_la_selection_ne_correspond_pas()
    {
        Assert.Null(_service.ReplaceCurrent(Text, Options("un", "1"), 3, 4));
        Assert.Null(_service.ReplaceCurrent(Text, Options(string.Empty, "1"), 0, 0));
    }

    [Fact]
    public void Remplace_toutes_les_occurrences_et_les_compte()
    {
        var (edit, count) = _service.ReplaceAll(Text, Options("un", "1"));

        Assert.Equal(2, count);
        Assert.Equal(new TextEdit(0, Text.Length, "1 deux 1", 0, 0), edit);
    }

    [Fact]
    public void Ne_produit_aucune_modification_sans_occurrence()
    {
        var (edit, count) = _service.ReplaceAll(Text, Options("trois", "3"));

        Assert.Null(edit);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Refuse_un_texte_ou_des_options_null()
    {
        Assert.Throws<ArgumentNullException>(() => _service.FindNext(null!, Options("a"), 0));
        Assert.Throws<ArgumentNullException>(() => _service.ReplaceAll(Text, null!));
    }
}
