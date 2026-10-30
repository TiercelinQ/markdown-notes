using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class OutlineViewModelTests : IDisposable
{
    private readonly EditorViewModel _editor = new(Substitute.For<IMarkdownFormattingService>());
    private readonly OutlineViewModel _outline;

    public OutlineViewModelTests() =>
        _outline = new OutlineViewModel(
            _editor,
            new OutlineService(),
            Substitute.For<ILocalizationService>(),
            Substitute.For<IFeedbackService>(),
            NullLogger<OutlineViewModel>.Instance);

    public void Dispose() => _outline.Dispose();

    [Fact]
    public async Task Construit_l_arbre_des_titres_du_texte_de_l_editeur()
    {
        _editor.Text = "# A\n\n## A1";

        await _outline.RefreshAsync(TestContext.Current.CancellationToken);

        var root = Assert.Single(_outline.Roots);
        Assert.Equal("A", root.Text);
        Assert.Equal("A1", Assert.Single(root.Children).Text);
        Assert.True(_outline.HasHeadings);
    }

    [Fact]
    public async Task Vide_l_arbre_quand_le_texte_n_a_plus_de_titre()
    {
        _editor.Text = "# A";
        await _outline.RefreshAsync(TestContext.Current.CancellationToken);

        _editor.Text = "texte";
        await _outline.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_outline.Roots);
        Assert.False(_outline.HasHeadings);
    }

    [Fact]
    public async Task Notifie_le_changement_de_HasHeadings()
    {
        var notified = new List<string?>();
        _outline.PropertyChanged += (_, e) => notified.Add(e.PropertyName);
        _editor.Text = "# A";

        await _outline.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Contains(nameof(OutlineViewModel.HasHeadings), notified);
    }

    [Fact]
    public async Task Se_met_a_jour_apres_une_modification_du_texte()
    {
        _editor.Text = "# Nouveau";

        await WaitForAsync(() => _outline.HasHeadings);

        Assert.Equal("Nouveau", Assert.Single(_outline.Roots).Text);
    }

    [Fact]
    public async Task Selectionne_le_titre_dans_l_editeur_sans_modifier_le_texte()
    {
        _editor.Text = "intro\n\n## Cible";
        await _outline.RefreshAsync(TestContext.Current.CancellationToken);
        var heading = _outline.Roots[0];

        _outline.GoToHeadingCommand.Execute(heading);

        var edit = _editor.PendingEdit;
        Assert.NotNull(edit);
        Assert.Equal(heading.Offset, edit.Start);
        Assert.Equal(0, edit.Length);
        Assert.Null(edit.Replacement);
        Assert.Equal(heading.Offset, edit.SelectionStart);
        Assert.Equal(heading.Length, edit.SelectionLength);
    }

    [Fact]
    public async Task Signale_l_index_du_titre_pour_l_apercu()
    {
        _editor.Text = "# A\n\n## B";
        await _outline.RefreshAsync(TestContext.Current.CancellationToken);
        var seen = new List<int>();
        _editor.HeadingSelected += (_, index) => seen.Add(index);

        _outline.GoToHeadingCommand.Execute(_outline.Roots[0].Children[0]);

        Assert.Equal([1], seen);
    }

    [Fact]
    public void Replie_et_deplie_le_plan()
    {
        Assert.True(_outline.IsPaneOpen);

        _outline.TogglePaneCommand.Execute(null);
        Assert.False(_outline.IsPaneOpen);

        _outline.TogglePaneCommand.Execute(null);
        Assert.True(_outline.IsPaneOpen);
    }

    [Fact]
    public void Ignore_une_navigation_sans_titre()
    {
        _outline.GoToHeadingCommand.Execute(null);

        Assert.Null(_editor.PendingEdit);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }
}
