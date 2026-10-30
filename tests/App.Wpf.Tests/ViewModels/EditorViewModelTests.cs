using System.Text;
using MarkdownNotes.Models;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class EditorViewModelTests
{
    private readonly IMarkdownFormattingService _formatting = Substitute.For<IMarkdownFormattingService>();
    private readonly EditorViewModel _editor;

    public EditorViewModelTests()
    {
        _editor = new EditorViewModel(_formatting);
    }

    private static MarkdownDocument Document(string text = "contenu", string? path = @"C:\docs\note.md") =>
        new(path, text, new UTF8Encoding(false));

    [Fact]
    public void Charge_un_document_sans_le_marquer_modifie()
    {
        _editor.Load(Document());

        Assert.Equal("contenu", _editor.Text);
        Assert.Equal(@"C:\docs\note.md", _editor.FilePath);
        Assert.Equal("note.md", _editor.FileName);
        Assert.False(_editor.IsDirty);
    }

    [Fact]
    public void Conserve_l_encodage_du_document_charge()
    {
        var encoding = new UTF8Encoding(true);

        _editor.Load(new MarkdownDocument(@"C:\docs\bom.md", "x", encoding));

        Assert.Same(encoding, _editor.Encoding);
    }

    [Fact]
    public void Marque_le_document_modifie_apres_une_frappe_puis_propre_au_retour_du_texte_d_origine()
    {
        _editor.Load(Document());

        _editor.Text = "contenu modifie";
        var dirty = _editor.IsDirty;
        _editor.Text = "contenu";

        Assert.True(dirty);
        Assert.False(_editor.IsDirty);
    }

    [Fact]
    public void Leve_l_evenement_TextChanged_a_chaque_changement_de_texte()
    {
        var raised = 0;
        _editor.TextChanged += (_, _) => raised++;

        _editor.Text = "a";
        _editor.Text = "ab";

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Calcule_la_ligne_et_la_colonne_depuis_la_position_du_curseur()
    {
        _editor.Text = "a\nbc";

        _editor.SelectionStart = 3;

        Assert.Equal(2, _editor.Line);
        Assert.Equal(2, _editor.Column);
    }

    [Fact]
    public void Remet_l_editeur_a_zero()
    {
        _editor.Load(Document());
        _editor.Text = "modifie";

        _editor.Reset();

        Assert.Equal(string.Empty, _editor.Text);
        Assert.Null(_editor.FilePath);
        Assert.Null(_editor.FileName);
        Assert.False(_editor.IsDirty);
    }

    [Fact]
    public void Marque_le_document_enregistre_et_memorise_le_chemin()
    {
        _editor.Text = "brouillon";
        Assert.True(_editor.IsDirty);

        _editor.MarkSaved(@"C:\docs\nouveau.md", "brouillon");

        Assert.False(_editor.IsDirty);
        Assert.Equal(@"C:\docs\nouveau.md", _editor.FilePath);
    }

    [Fact]
    public void Reste_modifie_si_le_texte_a_change_pendant_l_enregistrement()
    {
        _editor.Text = "v2";

        _editor.MarkSaved(@"C:\docs\nouveau.md", "v1");

        Assert.True(_editor.IsDirty);
    }

    [Fact]
    public void Transmet_la_selection_au_service_de_format_et_publie_la_modification()
    {
        var edit = new TextEdit(0, 3, "**abc**", 2, 3);
        _formatting.Apply(FormatKind.Bold, "abc", 0, 3).Returns(edit);
        _editor.Text = "abc";
        _editor.SelectionStart = 0;
        _editor.SelectionLength = 3;

        _editor.FormatCommand.Execute(FormatKind.Bold);

        Assert.Equal(edit, _editor.PendingEdit);
    }

    [Fact]
    public void Refuse_un_document_null()
    {
        Assert.Throws<ArgumentNullException>(() => _editor.Load(null!));
    }
}
