using System.IO;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class FolderServiceTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly FolderService _service = new(NullLogger<FolderService>.Instance);

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task Liste_les_dossiers_puis_les_fichiers_markdown_tries_par_nom()
    {
        Directory.CreateDirectory(_folder.Combine("beta"));
        Directory.CreateDirectory(_folder.Combine("Alpha"));
        _folder.WriteFile("zebre.md", "z");
        _folder.WriteFile("abeille.markdown", "a");
        _folder.WriteFile("image.png", "x");
        _folder.WriteFile("rapport.docx", "x");

        var result = await _service.ListChildrenAsync(_folder.Path, _folder.Path, TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
        Assert.Equal(
            [("Alpha", true), ("beta", true), ("abeille.markdown", false), ("zebre.md", false)],
            result.Data!.Select(n => (n.Name, n.IsDirectory)));
    }

    [Fact]
    public async Task Ecarte_les_elements_caches()
    {
        var hidden = _folder.WriteFile("cache.md", "x");
        File.SetAttributes(hidden, FileAttributes.Hidden);
        _folder.WriteFile("visible.md", "x");

        var result = await _service.ListChildrenAsync(_folder.Path, _folder.Path, TestContext.Current.CancellationToken);

        Assert.Equal(["visible.md"], result.Data!.Select(n => n.Name));
    }

    [Fact]
    public async Task Liste_un_sous_dossier_de_la_racine_avec_les_chemins_complets()
    {
        var file = _folder.WriteFile(Path.Combine("sous", "note.md"), "x");

        var result = await _service.ListChildrenAsync(_folder.Combine("sous"), _folder.Path, TestContext.Current.CancellationToken);

        Assert.Equal([new FolderNode("note.md", file, IsDirectory: false)], result.Data);
    }

    [Fact]
    public async Task Refuse_un_dossier_hors_de_la_racine_ouverte()
    {
        using var other = new TempFolder();

        var result = await _service.ListChildrenAsync(other.Path, _folder.Path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_InvalidPath, result.Error.Message);
    }

    [Fact]
    public async Task Refuse_un_dossier_voisin_dont_le_nom_commence_comme_la_racine()
    {
        var root = _folder.Combine("racine");
        Directory.CreateDirectory(root);
        var sibling = _folder.Combine("racine-voisin");
        Directory.CreateDirectory(sibling);

        var result = await _service.ListChildrenAsync(sibling, root, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(Strings.Error_InvalidPath, result.Error!.Message);
    }

    [Fact]
    public async Task Refuse_un_chemin_vide()
    {
        var result = await _service.ListChildrenAsync(string.Empty, _folder.Path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(Strings.Error_InvalidPath, result.Error!.Message);
    }

    [Fact]
    public async Task Retourne_une_erreur_danger_pour_un_dossier_inexistant()
    {
        var result = await _service.ListChildrenAsync(_folder.Combine("absent"), _folder.Path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Danger, result.Error!.Type);
        Assert.Equal(Strings.Error_FolderReadFailed, result.Error.Message);
    }

    [Fact]
    public async Task Annule_le_listage_quand_le_jeton_est_annule()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ListChildrenAsync(_folder.Path, _folder.Path, cts.Token));
    }
}
