using System.Globalization;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class FindReplaceViewModelTests
{
    private readonly EditorViewModel _editor = new(Substitute.For<IMarkdownFormattingService>());
    private readonly FindReplaceViewModel _find;

    public FindReplaceViewModelTests()
    {
        _find = new FindReplaceViewModel(_editor, new TextSearchService(), Substitute.For<ILocalizationService>());
        _editor.Text = "un deux un";
    }

    [Fact]
    public void Ouvre_la_barre_en_mode_recherche_ou_en_mode_remplacement()
    {
        _find.Show(replace: false);
        var findOnly = (_find.IsVisible, _find.IsReplaceVisible);
        _find.Show(replace: true);

        Assert.Equal((true, false), findOnly);
        Assert.True(_find.IsReplaceVisible);
    }

    [Fact]
    public void Ferme_la_barre()
    {
        _find.Show(replace: true);

        _find.CloseCommand.Execute(null);

        Assert.False(_find.IsVisible);
    }

    [Fact]
    public void Selectionne_la_premiere_occurrence_sans_modifier_le_texte()
    {
        _find.Query = "un";

        _find.FindNextCommand.Execute(null);

        Assert.Equal(new TextEdit(0, 2, null, 0, 2), _editor.PendingEdit);
        Assert.Equal("un deux un", _editor.Text);
    }

    [Fact]
    public void Continue_apres_la_selection_courante()
    {
        _find.Query = "un";
        _editor.SelectionStart = 0;
        _editor.SelectionLength = 2;

        _find.FindNextCommand.Execute(null);

        Assert.Equal(new TextEdit(8, 2, null, 8, 2), _editor.PendingEdit);
    }

    [Fact]
    public void Recule_vers_l_occurrence_precedente()
    {
        _find.Query = "un";
        _editor.SelectionStart = 8;
        _editor.SelectionLength = 2;

        _find.FindPreviousCommand.Execute(null);

        Assert.Equal(new TextEdit(0, 2, null, 0, 2), _editor.PendingEdit);
    }

    [Fact]
    public void Signale_l_absence_de_resultat()
    {
        _find.Query = "trois";

        _find.FindNextCommand.Execute(null);

        Assert.Equal(Strings.Find_NoMatch, _find.StatusText);
        Assert.Null(_editor.PendingEdit);
    }

    [Fact]
    public void Efface_le_statut_quand_la_requete_change()
    {
        _find.Query = "trois";
        _find.FindNextCommand.Execute(null);

        _find.Query = "un";

        Assert.Equal(string.Empty, _find.StatusText);
    }

    [Fact]
    public void Remplace_l_occurrence_selectionnee()
    {
        _find.Query = "un";
        _find.Replacement = "1";
        _editor.SelectionStart = 0;
        _editor.SelectionLength = 2;

        _find.ReplaceCommand.Execute(null);

        Assert.Equal(new TextEdit(0, 2, "1", 7, 2), _editor.PendingEdit);
    }

    [Fact]
    public void Cherche_la_prochaine_occurrence_quand_rien_n_est_selectionne_au_remplacement()
    {
        _find.Query = "un";
        _find.Replacement = "1";

        _find.ReplaceCommand.Execute(null);

        Assert.Equal(new TextEdit(0, 2, null, 0, 2), _editor.PendingEdit);
    }

    [Fact]
    public void Remplace_tout_et_indique_le_nombre_de_remplacements()
    {
        _find.Query = "un";
        _find.Replacement = "1";

        _find.ReplaceAllCommand.Execute(null);

        Assert.Equal("1 deux 1", _editor.PendingEdit!.Replacement);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.Find_Replaced, 2), _find.StatusText);
    }

    [Fact]
    public void Signale_l_absence_d_occurrence_au_remplacement_global()
    {
        _find.Query = "trois";

        _find.ReplaceAllCommand.Execute(null);

        Assert.Equal(Strings.Find_NoMatch, _find.StatusText);
        Assert.Null(_editor.PendingEdit);
    }

    [Fact]
    public void Tient_compte_de_la_casse_quand_elle_est_demandee()
    {
        _editor.Text = "Un un";
        _find.Query = "un";
        _find.MatchCase = true;

        _find.FindNextCommand.Execute(null);

        Assert.Equal(new TextEdit(3, 2, null, 3, 2), _editor.PendingEdit);
    }
}
