using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class MarkdownRenderServiceTests
{
    private readonly MarkdownRenderService _service = new();

    private async Task<string> RenderAsync(string markdown)
    {
        var result = await _service.RenderHtmlAsync(markdown, TestContext.Current.CancellationToken);
        Assert.True(result.Ok);
        return result.Data!;
    }

    [Fact]
    public async Task Convertit_un_titre_en_html()
    {
        var page = await RenderAsync("# Titre");

        Assert.Contains("<h1", page, StringComparison.Ordinal);
        Assert.Contains("Titre</h1>", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Donne_un_identifiant_d_ancre_aux_titres()
    {
        var page = await RenderAsync("## Mon titre");

        Assert.Contains("<h2 id=\"mon-titre\">", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rend_un_bloc_mermaid_dans_un_conteneur_pour_la_bibliotheque_de_diagrammes()
    {
        var page = await RenderAsync("```mermaid\nflowchart TD\n  A --> B\n```");

        Assert.Contains("class=\"mermaid\"", page, StringComparison.Ordinal);
        Assert.Contains("flowchart TD", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prend_en_charge_les_tableaux_GFM()
    {
        var page = await RenderAsync("| a | b |\n|---|---|\n| 1 | 2 |");

        Assert.Contains("<table", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Place_le_corps_entre_les_marqueurs_partages_avec_la_vue()
    {
        var page = await RenderAsync("texte");

        var start = page.IndexOf(AppConfig.PreviewBodyStart, StringComparison.Ordinal);
        var end = page.IndexOf(AppConfig.PreviewBodyEnd, StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start);
        Assert.Contains("<p>texte</p>", page[start..end], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Embarque_la_politique_de_securite_la_base_et_la_feuille_de_style()
    {
        var page = await RenderAsync("texte");

        Assert.Contains("Content-Security-Policy", page, StringComparison.Ordinal);
        Assert.Contains("default-src 'none'", page, StringComparison.Ordinal); Assert.Contains($"<base href=\"https://{AppConfig.PreviewVirtualHost}/\">", page, StringComparison.Ordinal);
        Assert.Contains("<style>", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Neutralise_le_html_brut_de_la_source()
    {
        var page = await RenderAsync("<script>alert(1)</script>");

        Assert.DoesNotContain("<script>alert", page, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Retourne_un_avertissement_quand_la_page_depasse_la_taille_maximale()
    {
        var result = await _service.RenderHtmlAsync(new string('a', AppConfig.PreviewMaxChars + 1), TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_PreviewTooLarge, result.Error.Message);
    }

    [Fact]
    public async Task Annule_le_rendu_quand_le_jeton_est_annule()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.RenderHtmlAsync("texte", cts.Token));
    }

    [Fact]
    public async Task Refuse_un_texte_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RenderHtmlAsync(null!, TestContext.Current.CancellationToken));
    }
}
