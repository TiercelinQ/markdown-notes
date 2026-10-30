using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class RecentFilesServiceTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly PreferencesService _preferences;
    private readonly RecentFilesService _service;

    public RecentFilesServiceTests()
    {
        _preferences = new PreferencesService(TestPaths.For(_folder.Path), NullLogger<PreferencesService>.Instance);
        _service = new RecentFilesService(_preferences);
    }

    public void Dispose()
    {
        _preferences.Dispose();
        _folder.Dispose();
    }

    [Fact]
    public async Task Place_le_dernier_fichier_ajoute_en_tete()
    {
        var first = _folder.WriteFile("a.md", "a");
        var second = _folder.WriteFile("b.md", "b");

        await _service.AddAsync(first, TestContext.Current.CancellationToken);
        await _service.AddAsync(second, TestContext.Current.CancellationToken);

        Assert.Equal([second, first], await _service.ListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Ne_duplique_pas_un_fichier_deja_present_sans_tenir_compte_de_la_casse()
    {
        var path = _folder.WriteFile("a.md", "a");
        var other = _folder.WriteFile("b.md", "b");

        await _service.AddAsync(path, TestContext.Current.CancellationToken);
        await _service.AddAsync(other, TestContext.Current.CancellationToken);
        await _service.AddAsync(path.ToUpperInvariant(), TestContext.Current.CancellationToken);

        Assert.Equal(2, _preferences.Current.RecentFiles.Count);
        Assert.Equal(path.ToUpperInvariant(), _preferences.Current.RecentFiles[0]);
    }

    [Fact]
    public async Task Ecarte_de_la_liste_les_fichiers_qui_n_existent_plus()
    {
        var present = _folder.WriteFile("present.md", "a");
        var missing = _folder.Combine("absent.md");

        await _service.AddAsync(missing, TestContext.Current.CancellationToken);
        await _service.AddAsync(present, TestContext.Current.CancellationToken);

        Assert.Equal([present], await _service.ListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Retire_un_fichier_de_la_liste()
    {
        var first = _folder.WriteFile("a.md", "a");
        var second = _folder.WriteFile("b.md", "b");
        await _service.AddAsync(first, TestContext.Current.CancellationToken);
        await _service.AddAsync(second, TestContext.Current.CancellationToken);

        await _service.RemoveAsync(first, TestContext.Current.CancellationToken);

        Assert.Equal([second], await _service.ListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Conserve_au_plus_le_nombre_configure_de_fichiers()
    {
        for (var i = 0; i < AppConfig.RecentFilesMax + 2; i++)
        {
            await _service.AddAsync(_folder.WriteFile($"{i}.md", "x"), TestContext.Current.CancellationToken);
        }

        Assert.Equal(AppConfig.RecentFilesMax, _preferences.Current.RecentFiles.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refuse_un_chemin_vide(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(path, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemoveAsync(path, TestContext.Current.CancellationToken));
    }
}
