using MarkdownNotes.Models;
using MarkdownNotes.Services;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class MarkdownFormattingServiceTests
{
    private readonly MarkdownFormattingService _service = new();

    [Fact]
    public void Entoure_la_selection_de_gras()
    {
        var edit = _service.Apply(FormatKind.Bold, "hello world", 6, 5);

        Assert.Equal(new TextEdit(6, 5, "**world**", 8, 5), edit);
    }

    [Fact]
    public void Insere_des_marqueurs_de_gras_vides_sans_selection_et_place_le_curseur_entre_eux()
    {
        var edit = _service.Apply(FormatKind.Bold, "abc", 3, 0);

        Assert.Equal(new TextEdit(3, 0, "****", 5, 0), edit);
    }

    [Fact]
    public void Retire_le_gras_d_une_selection_deja_entouree()
    {
        var edit = _service.Apply(FormatKind.Bold, "**world**", 2, 5);

        Assert.Equal(new TextEdit(0, 9, "world", 0, 5), edit);
    }

    [Fact]
    public void Entoure_la_selection_d_italique()
    {
        var edit = _service.Apply(FormatKind.Italic, "un mot", 3, 3);

        Assert.Equal(new TextEdit(3, 3, "*mot*", 4, 3), edit);
    }

    [Fact]
    public void Retire_l_italique_d_une_selection_deja_entouree()
    {
        var edit = _service.Apply(FormatKind.Italic, "*x*", 1, 1);

        Assert.Equal(new TextEdit(0, 3, "x", 0, 1), edit);
    }

    [Fact]
    public void Ajoute_l_italique_autour_d_un_texte_deja_en_gras_au_lieu_de_le_retirer()
    {
        var edit = _service.Apply(FormatKind.Italic, "**x**", 2, 1);

        Assert.Equal(new TextEdit(2, 1, "*x*", 3, 1), edit);
    }

    [Fact]
    public void Ajoute_un_titre_de_niveau_1_a_la_ligne_du_curseur()
    {
        var edit = _service.Apply(FormatKind.Heading1, "Titre", 0, 0);

        Assert.Equal(new TextEdit(0, 5, "# Titre", 0, 7), edit);
    }

    [Fact]
    public void Retire_le_titre_quand_le_meme_niveau_est_deja_applique()
    {
        var edit = _service.Apply(FormatKind.Heading1, "# Titre", 0, 0);

        Assert.Equal(new TextEdit(0, 7, "Titre", 0, 5), edit);
    }

    [Fact]
    public void Change_le_niveau_d_un_titre_existant()
    {
        var edit = _service.Apply(FormatKind.Heading2, "# Titre", 0, 0);

        Assert.Equal("## Titre", edit.Replacement);
    }

    [Fact]
    public void Ne_transforme_que_la_ligne_du_curseur()
    {
        var edit = _service.Apply(FormatKind.Heading1, "x\ny\nz", 3, 0);

        Assert.Equal(new TextEdit(2, 1, "# y", 2, 3), edit);
    }

    [Fact]
    public void Transforme_chaque_ligne_selectionnee_en_liste_a_puces()
    {
        var edit = _service.Apply(FormatKind.BulletList, "a\nb", 0, 3);

        Assert.Equal(new TextEdit(0, 3, "- a\n- b", 0, 7), edit);
    }

    [Fact]
    public void Ignore_le_retour_a_la_ligne_final_de_la_selection()
    {
        var edit = _service.Apply(FormatKind.BulletList, "a\nb\n", 0, 4);

        Assert.Equal(new TextEdit(0, 3, "- a\n- b", 0, 7), edit);
    }

    [Fact]
    public void Retire_les_puces_en_conservant_les_lignes_vides()
    {
        var edit = _service.Apply(FormatKind.BulletList, "- a\n\n- b", 0, 8);

        Assert.Equal("a\n\nb", edit.Replacement);
    }

    [Fact]
    public void Numerote_les_lignes_en_sautant_les_lignes_vides()
    {
        var edit = _service.Apply(FormatKind.NumberedList, "a\n\nb", 0, 4);

        Assert.Equal("1. a\n\n2. b", edit.Replacement);
    }

    [Fact]
    public void Retire_la_numerotation_existante()
    {
        var edit = _service.Apply(FormatKind.NumberedList, "1. a\n2. b", 0, 9);

        Assert.Equal("a\nb", edit.Replacement);
    }

    [Fact]
    public void Remplace_une_puce_par_une_numerotation()
    {
        var edit = _service.Apply(FormatKind.NumberedList, "- a", 0, 0);

        Assert.Equal("1. a", edit.Replacement);
    }

    [Fact]
    public void Ajoute_puis_retire_la_citation()
    {
        var added = _service.Apply(FormatKind.Quote, "a", 0, 0);
        var removed = _service.Apply(FormatKind.Quote, "> a", 0, 0);

        Assert.Equal("> a", added.Replacement);
        Assert.Equal("a", removed.Replacement);
    }

    [Fact]
    public void Insere_un_lien_vide_et_place_le_curseur_dans_les_crochets()
    {
        var edit = _service.Apply(FormatKind.Link, "ab", 2, 0);

        Assert.Equal(new TextEdit(2, 0, "[]()", 3, 0), edit);
    }

    [Fact]
    public void Transforme_la_selection_en_lien_et_selectionne_l_adresse_a_remplacer()
    {
        var edit = _service.Apply(FormatKind.Link, "site", 0, 4);

        Assert.Equal(new TextEdit(0, 4, "[site](url)", 7, 3), edit);
    }

    [Fact]
    public void Entoure_une_selection_d_une_ligne_de_code_en_ligne()
    {
        var edit = _service.Apply(FormatKind.Code, "var x", 4, 1);

        Assert.Equal(new TextEdit(4, 1, "`x`", 5, 1), edit);
    }

    [Fact]
    public void Entoure_une_selection_multiligne_d_un_bloc_de_code()
    {
        var newline = Environment.NewLine;

        var edit = _service.Apply(FormatKind.Code, "a\nb", 0, 3);

        Assert.Equal("```" + newline + "a\nb" + newline + "```", edit.Replacement);
        Assert.Equal(3 + newline.Length, edit.SelectionStart);
    }

    [Fact]
    public void Ramene_une_selection_hors_du_texte_dans_ses_limites()
    {
        var edit = _service.Apply(FormatKind.Bold, "abc", 99, 5);

        Assert.Equal(new TextEdit(3, 0, "****", 5, 0), edit);
    }

    [Fact]
    public void Refuse_une_action_de_format_inconnue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Apply((FormatKind)99, "abc", 0, 0));
    }

    [Fact]
    public void Refuse_un_texte_null()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Apply(FormatKind.Bold, null!, 0, 0));
    }
}
