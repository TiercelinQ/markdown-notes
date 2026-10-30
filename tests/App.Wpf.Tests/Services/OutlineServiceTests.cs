using MarkdownNotes.Models;
using MarkdownNotes.Services;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class OutlineServiceTests
{
    private readonly OutlineService _service = new();

    private async Task<IReadOnlyList<HeadingNode>> ExtractAsync(string markdown) =>
        await _service.ExtractAsync(markdown, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Retourne_une_liste_vide_sans_titre()
    {
        var roots = await ExtractAsync("simple texte");

        Assert.Empty(roots);
    }

    [Fact]
    public async Task Imbrique_les_titres_selon_leur_niveau()
    {
        var roots = await ExtractAsync("# A\n\n## A1\n\n## A2\n\n### A2a\n\n# B");

        Assert.Equal(["A", "B"], roots.Select(r => r.Text));
        Assert.Equal([0, 4], roots.Select(r => r.Index));
        Assert.Equal([1, 2], roots[0].Children.Select(c => c.Index));
        Assert.Equal(["A1", "A2"], roots[0].Children.Select(c => c.Text));
        Assert.Equal(["A2a"], roots[0].Children[1].Children.Select(c => c.Text));
        Assert.Empty(roots[1].Children);
    }

    [Fact]
    public async Task Rattache_un_titre_au_plus_proche_niveau_inferieur_quand_un_niveau_est_saute()
    {
        var roots = await ExtractAsync("# A\n\n### A1a\n\n## A2");

        Assert.Equal(["A1a", "A2"], roots[0].Children.Select(c => c.Text));
    }

    [Fact]
    public async Task Garde_un_titre_de_niveau_eleve_comme_racine_quand_il_ouvre_le_document()
    {
        var roots = await ExtractAsync("## A\n\n# B");

        Assert.Equal(["A", "B"], roots.Select(r => r.Text));
    }

    [Fact]
    public async Task Ignore_les_diese_dans_un_bloc_de_code()
    {
        var roots = await ExtractAsync("# A\n\n```\n# pas un titre\n```");

        Assert.Equal(["A"], roots.Select(r => r.Text));
    }

    [Fact]
    public async Task Reconnait_les_titres_soulignes()
    {
        var roots = await ExtractAsync("Titre un\n========\n\nTitre deux\n----------");

        Assert.Equal(["Titre un"], roots.Select(r => r.Text));
        Assert.Equal(["Titre deux"], roots[0].Children.Select(c => c.Text));
    }

    [Fact]
    public async Task Extrait_le_texte_brut_sans_la_mise_en_forme()
    {
        var roots = await ExtractAsync("# Du **gras** et `code`");

        Assert.Equal("Du gras et code", roots[0].Text);
    }

    [Fact]
    public async Task Ignore_un_titre_vide()
    {
        var roots = await ExtractAsync("#\n\n# A");

        Assert.Equal(["A"], roots.Select(r => r.Text));
    }

    [Fact]
    public async Task Donne_la_position_du_titre_dans_le_texte()
    {
        const string Text = "intro\n\n## Cible\n";

        var roots = await ExtractAsync(Text);

        Assert.Equal(Text.IndexOf("## Cible", StringComparison.Ordinal), roots[0].Offset);
        Assert.Equal("## Cible".Length, roots[0].Length);
        Assert.Equal(2, roots[0].Level);
    }

    [Fact]
    public async Task Leve_une_annulation_quand_le_jeton_est_annule()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ExtractAsync("# A", cts.Token));
    }

    [Fact]
    public async Task Refuse_un_texte_nul()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ExtractAsync(null!, TestContext.Current.CancellationToken));
    }
}
