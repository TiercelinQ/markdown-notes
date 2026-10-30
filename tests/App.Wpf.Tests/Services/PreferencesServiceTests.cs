using System.IO;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class PreferencesServiceTests : IDisposable
{
    private readonly TempFolder _folder = new();

    private PreferencesService CreateService(string? userData = null) =>
        new(TestPaths.For(userData ?? _folder.Path), NullLogger<PreferencesService>.Instance);

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task Garde_les_valeurs_par_defaut_sans_fichier()
    {
        using var service = CreateService();

        var result = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
        Assert.Equal(new UserPreferences(), service.Current);
    }

    [Fact]
    public async Task Relit_ce_qui_a_ete_ecrit()
    {
        using (var writer = CreateService())
        {
            await writer.UpdateAsync(
                p => p with { Theme = AppTheme.Dark, Culture = "en", ViewMode = ViewMode.PreviewOnly, WindowWidth = 900 },
                TestContext.Current.CancellationToken);
        }

        using var reader = CreateService();
        var result = await reader.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
        Assert.Equal(AppTheme.Dark, reader.Current.Theme);
        Assert.Equal("en", reader.Current.Culture);
        Assert.Equal(ViewMode.PreviewOnly, reader.Current.ViewMode);
        Assert.Equal(900, reader.Current.WindowWidth);
    }

    [Fact]
    public async Task Ecrit_sans_laisser_de_fichier_temporaire()
    {
        using var service = CreateService();

        await service.UpdateAsync(p => p with { ExplorerVisible = false }, TestContext.Current.CancellationToken);

        Assert.Equal([AppConfig.PreferencesFileName], Directory.GetFiles(_folder.Path).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Retourne_un_avertissement_et_les_valeurs_par_defaut_sur_un_fichier_corrompu()
    {
        _folder.WriteFile(AppConfig.PreferencesFileName, "{ pas du json");
        using var service = CreateService();

        var result = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Warning, result.Error!.Type);
        Assert.Equal(Strings.Error_PreferencesLoad, result.Error.Message);
        Assert.Equal(new UserPreferences(), service.Current);
    }

    [Fact]
    public async Task Remplace_une_culture_non_prise_en_charge_par_la_culture_par_defaut()
    {
        _folder.WriteFile(AppConfig.PreferencesFileName, """{ "Culture": "de" }""");
        using var service = CreateService();

        await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppConfig.DefaultCulture, service.Current.Culture);
    }

    [Fact]
    public async Task Ecarte_les_dimensions_de_fenetre_invalides()
    {
        _folder.WriteFile(AppConfig.PreferencesFileName, """{ "WindowWidth": -10, "WindowHeight": 0, "WindowLeft": 12 }""");
        using var service = CreateService();

        await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Null(service.Current.WindowWidth);
        Assert.Null(service.Current.WindowHeight);
        Assert.Equal(12, service.Current.WindowLeft);
    }

    [Fact]
    public async Task Ramene_un_mode_d_affichage_inconnu_a_cote_a_cote()
    {
        using var service = CreateService();

        await service.UpdateAsync(p => p with { ViewMode = (ViewMode)99 }, TestContext.Current.CancellationToken);

        Assert.Equal(ViewMode.Split, service.Current.ViewMode);
    }

    [Fact]
    public async Task Limite_les_fichiers_recents_au_maximum_configure()
    {
        using var service = CreateService();
        var many = Enumerable.Range(0, AppConfig.RecentFilesMax + 5).Select(i => $@"C:\docs\{i}.md").ToList();

        await service.UpdateAsync(p => p with { RecentFiles = many }, TestContext.Current.CancellationToken);

        Assert.Equal(AppConfig.RecentFilesMax, service.Current.RecentFiles.Count);
    }

    [Fact]
    public async Task Retourne_une_erreur_danger_quand_l_ecriture_echoue()
    {
        var blocker = _folder.WriteFile("bloqueur", "ce fichier empeche la creation du dossier");
        using var service = CreateService(Path.Combine(blocker, "sous-dossier"));

        var result = await service.UpdateAsync(p => p with { ExplorerVisible = false }, TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(FeedbackType.Danger, result.Error!.Type);
        Assert.Equal(Strings.Error_PreferencesSave, result.Error.Message);
    }

    [Fact]
    public async Task Annule_le_chargement_quand_le_jeton_est_annule()
    {
        using var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LoadAsync(cts.Token));
    }

    [Fact]
    public async Task Refuse_une_fonction_de_mise_a_jour_null()
    {
        using var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(null!, TestContext.Current.CancellationToken));
    }
}
