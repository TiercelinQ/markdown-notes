using MarkdownNotes.Models;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class ExplorerViewModelTests
{
    private const string Root = @"C:\docs";

    private readonly IFolderService _folders = Substitute.For<IFolderService>();
    private readonly IFeedbackService _feedback = Substitute.For<IFeedbackService>();
    private readonly ExplorerViewModel _explorer;

    public ExplorerViewModelTests()
    {
        _explorer = new ExplorerViewModel(_folders, _feedback, Substitute.For<ILocalizationService>());
    }

    private void Returns(string folder, params FolderNode[] nodes) =>
        _folders.ListChildrenAsync(folder, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Success(nodes)));

    private void Fails(string folder) =>
        _folders.ListChildrenAsync(folder, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Failure(new ResultError(FeedbackType.Danger, "Echec"))));

    [Fact]
    public void Replie_et_deplie_l_explorateur()
    {
        Assert.True(_explorer.IsPaneOpen);

        _explorer.TogglePaneCommand.Execute(null);
        Assert.False(_explorer.IsPaneOpen);

        _explorer.TogglePaneCommand.Execute(null);
        Assert.True(_explorer.IsPaneOpen);
    }

    [Fact]
    public async Task Remplit_la_racine_avec_les_noeuds_du_dossier_ouvert()
    {
        Returns(Root, new FolderNode("sous", @"C:\docs\sous", true), new FolderNode("a.md", @"C:\docs\a.md", false));

        var opened = await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);

        Assert.True(opened);
        Assert.Equal(["sous", "a.md"], _explorer.Roots.Select(n => n.Name));
        Assert.Equal(Root, _explorer.RootPath);
        Assert.True(_explorer.HasFolder);
    }

    [Fact]
    public async Task Affiche_l_erreur_et_garde_l_etat_precedent_quand_l_ouverture_echoue()
    {
        Fails(Root);

        var opened = await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);

        Assert.False(opened);
        Assert.False(_explorer.HasFolder);
        Assert.Empty(_explorer.Roots);
        _feedback.Received(1).Show(Arg.Is<ResultError>(e => e.Type == FeedbackType.Danger));
    }

    [Fact]
    public async Task Autorise_l_actualisation_seulement_quand_un_dossier_est_ouvert()
    {
        Returns(Root);
        var before = _explorer.RefreshCommand.CanExecute(null);

        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);

        Assert.False(before);
        Assert.True(_explorer.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task Relit_le_dossier_racine_a_l_actualisation()
    {
        Returns(Root, new FolderNode("a.md", @"C:\docs\a.md", false));
        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);
        Returns(Root, new FolderNode("a.md", @"C:\docs\a.md", false), new FolderNode("b.md", @"C:\docs\b.md", false));

        await _explorer.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(["a.md", "b.md"], _explorer.Roots.Select(n => n.Name));
    }

    [Fact]
    public async Task Charge_les_enfants_d_un_noeud_deplie()
    {
        Returns(Root, new FolderNode("sous", @"C:\docs\sous", true));
        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);
        Returns(@"C:\docs\sous", new FolderNode("n.md", @"C:\docs\sous\n.md", false));
        var node = _explorer.Roots[0];

        await _explorer.ExpandNodeCommand.ExecuteAsync(node);

        Assert.True(node.IsLoaded);
        Assert.Equal(["n.md"], node.Children.Select(c => c.Name));
    }

    [Fact]
    public async Task Affiche_l_erreur_et_vide_les_enfants_quand_le_depliage_echoue()
    {
        Returns(Root, new FolderNode("sous", @"C:\docs\sous", true));
        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);
        Fails(@"C:\docs\sous");
        var node = _explorer.Roots[0];

        await _explorer.ExpandNodeCommand.ExecuteAsync(node);

        Assert.Empty(node.Children);
        _feedback.Received(1).Show(Arg.Is<ResultError>(e => e.Type == FeedbackType.Danger));
    }

    [Fact]
    public async Task Ne_fait_rien_pour_un_noeud_null_ou_sans_dossier_ouvert()
    {
        await _explorer.ExpandNodeCommand.ExecuteAsync(null);

        await _folders.DidNotReceive().ListChildrenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leve_FileActivated_pour_un_fichier_et_pas_pour_un_dossier()
    {
        Returns(Root, new FolderNode("sous", @"C:\docs\sous", true), new FolderNode("a.md", @"C:\docs\a.md", false));
        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);
        var activated = new List<string>();
        _explorer.FileActivated += (_, path) => activated.Add(path);

        _explorer.ActivateNodeCommand.Execute(_explorer.Roots[0]);
        _explorer.ActivateNodeCommand.Execute(_explorer.Roots[1]);

        Assert.Equal([@"C:\docs\a.md"], activated);
    }
}
