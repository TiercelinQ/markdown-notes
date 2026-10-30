using System.IO;
using System.Text;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class DocumentServiceTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly DocumentService _service = new(NullLogger<DocumentService>.Instance);

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task Relit_le_texte_ecrit_avec_son_chemin_complet()
    {
        var path = _folder.Combine("note.md");

        var write = await _service.WriteAsync(path, "# Titre\n\nCorps accentue: éàü", new UTF8Encoding(false), TestContext.Current.CancellationToken);
        var read = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.True(write.Ok);
        Assert.True(read.Ok);
        Assert.Equal("# Titre\n\nCorps accentue: éàü", read.Data!.Text);
        Assert.Equal(path, read.Data.Path);
    }

    [Fact]
    public async Task Detecte_un_fichier_avec_BOM_et_le_reecrit_avec_BOM()
    {
        var path = _folder.Combine("bom.md");
        await _service.WriteAsync(path, "texte", new UTF8Encoding(true), TestContext.Current.CancellationToken);

        var read = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal([0xEF, 0xBB, 0xBF], (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).Take(3));
        Assert.Equal(3, read.Data!.Encoding.GetPreamble().Length);
    }

    [Fact]
    public async Task Lit_un_fichier_sans_BOM_comme_UTF8_sans_BOM()
    {
        var path = _folder.WriteFile("plain.md", "texte");

        var read = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.Empty(read.Data!.Encoding.GetPreamble());
    }

    [Fact]
    public async Task Retourne_un_avertissement_pour_un_fichier_absent()
    {
        var result = await _service.ReadAsync(_folder.Combine("absent.md"), TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_FileNotFound, result.Error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refuse_un_chemin_vide_en_lecture(string path)
    {
        var result = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_InvalidPath, result.Error.Message);
    }

    [Fact]
    public async Task Refuse_une_extension_non_prise_en_charge_en_lecture()
    {
        var path = _folder.WriteFile("programme.exe", "x");

        var result = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_InvalidPath, result.Error.Message);
    }

    [Fact]
    public async Task Refuse_un_fichier_plus_gros_que_la_limite()
    {
        var path = _folder.Combine("enorme.md");
        await using (var stream = File.Create(path))
        {
            stream.SetLength(AppConfig.MaxDocumentBytes + 1);
        }

        var result = await _service.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_FileTooLarge, result.Error.Message);
    }

    [Fact]
    public async Task Remplace_le_contenu_d_un_fichier_existant_sans_laisser_de_temporaire()
    {
        var path = _folder.WriteFile("note.md", "ancien");

        var result = await _service.WriteAsync(path, "nouveau", new UTF8Encoding(false), TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
        Assert.Equal("nouveau", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal([path], Directory.GetFiles(_folder.Path));
    }

    [Fact]
    public async Task Retourne_une_erreur_danger_quand_le_dossier_d_ecriture_n_existe_pas()
    {
        var path = _folder.Combine("absent", "note.md");

        var result = await _service.WriteAsync(path, "texte", new UTF8Encoding(false), TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Danger, result.Error!.Type);
        Assert.Equal(Strings.Error_WriteFailed, result.Error.Message);
    }

    [Fact]
    public async Task Refuse_d_ecrire_sous_une_extension_non_prise_en_charge()
    {
        var path = _folder.Combine("script.exe");

        var result = await _service.WriteAsync(path, "texte", new UTF8Encoding(false), TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.False(File.Exists(path));
    }
}
