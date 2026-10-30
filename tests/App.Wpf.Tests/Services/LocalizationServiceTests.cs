using MarkdownNotes.Services;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly LocalizationService _service = new();

    // The culture is process-wide: put it back so other tests read French messages.
    public void Dispose() => _service.SetCulture(AppConfig.DefaultCulture);

    [Fact]
    public void Demarre_dans_la_culture_par_defaut()
    {
        Assert.Equal(AppConfig.DefaultCulture, _service.CurrentCulture);
    }

    [Fact]
    public void Retourne_le_libelle_dans_la_culture_choisie()
    {
        _service.SetCulture("fr");
        var french = _service["Editor_Name"];
        _service.SetCulture("en");
        var english = _service["Editor_Name"];

        Assert.Equal("Éditeur Markdown", french);
        Assert.Equal("Markdown editor", english);
    }

    [Fact]
    public void Accepte_le_code_de_culture_sans_tenir_compte_de_la_casse()
    {
        _service.SetCulture("EN");

        Assert.Equal("en", _service.CurrentCulture);
    }

    [Fact]
    public void Revient_a_la_culture_par_defaut_pour_une_culture_non_prise_en_charge()
    {
        _service.SetCulture("en");

        _service.SetCulture("de");

        Assert.Equal(AppConfig.DefaultCulture, _service.CurrentCulture);
    }

    [Fact]
    public void Notifie_l_indexeur_et_la_culture_courante_au_changement_de_langue()
    {
        var changed = new List<string?>();
        _service.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _service.SetCulture("en");

        Assert.Contains("Item[]", changed);
        Assert.Contains(nameof(LocalizationService.CurrentCulture), changed);
    }

    [Fact]
    public void Retourne_la_cle_quand_le_libelle_est_inconnu()
    {
        Assert.Equal("Cle_Inexistante", _service["Cle_Inexistante"]);
    }
}
