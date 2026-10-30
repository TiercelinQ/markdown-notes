using MarkdownNotes.Models;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class FolderNodeViewModelTests
{
    private const string Root = @"C:\docs";

    private readonly IFolderService _folders = Substitute.For<IFolderService>();
    private readonly ExplorerViewModel _explorer;

    public FolderNodeViewModelTests()
    {
        _explorer = new ExplorerViewModel(_folders, Substitute.For<IFeedbackService>(), Substitute.For<ILocalizationService>());
    }

    private async Task<FolderNodeViewModel> OpenSingleAsync(FolderNode node)
    {
        _folders.ListChildrenAsync(Root, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Success([node])));
        await _explorer.OpenFolderAsync(Root, TestContext.Current.CancellationToken);
        return _explorer.Roots[0];
    }

    [Fact]
    public async Task Un_dossier_contient_un_enfant_factice_avant_son_chargement()
    {
        var node = await OpenSingleAsync(new FolderNode("sous", @"C:\docs\sous", true));

        Assert.False(node.IsLoaded);
        Assert.Single(node.Children);
        Assert.True(node.Children[0].IsPlaceholder);
    }

    [Fact]
    public async Task Un_fichier_n_a_aucun_enfant()
    {
        var node = await OpenSingleAsync(new FolderNode("a.md", @"C:\docs\a.md", false));

        Assert.Empty(node.Children);
        Assert.False(node.IsDirectory);
    }

    [Fact]
    public async Task Remplace_l_enfant_factice_par_les_enfants_charges()
    {
        var node = await OpenSingleAsync(new FolderNode("sous", @"C:\docs\sous", true));
        var child = new FolderNodeViewModel(new FolderNode("n.md", @"C:\docs\sous\n.md", false), _explorer);

        node.SetChildren([child]);

        Assert.True(node.IsLoaded);
        Assert.Equal([child], node.Children);
    }

    [Fact]
    public async Task Charge_les_enfants_une_seule_fois_au_premier_depliage()
    {
        var node = await OpenSingleAsync(new FolderNode("sous", @"C:\docs\sous", true));
        _folders.ListChildrenAsync(@"C:\docs\sous", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Success([new FolderNode("n.md", @"C:\docs\sous\n.md", false)])));

        node.IsExpanded = true;
        node.IsExpanded = false;
        node.IsExpanded = true;

        await _folders.Received(1).ListChildrenAsync(@"C:\docs\sous", Root, Arg.Any<CancellationToken>());
        Assert.Equal(["n.md"], node.Children.Select(c => c.Name));
    }

    [Fact]
    public async Task Active_le_fichier_quand_il_est_selectionne()
    {
        var node = await OpenSingleAsync(new FolderNode("a.md", @"C:\docs\a.md", false));
        string? activated = null;
        _explorer.FileActivated += (_, path) => activated = path;

        node.IsSelected = true;

        Assert.Equal(@"C:\docs\a.md", activated);
    }

    [Fact]
    public async Task N_active_pas_un_dossier_selectionne()
    {
        var node = await OpenSingleAsync(new FolderNode("sous", @"C:\docs\sous", true));
        var raised = false;
        _explorer.FileActivated += (_, _) => raised = true;

        node.IsSelected = true;

        Assert.False(raised);
    }

    [Fact]
    public void Refuse_un_noeud_modele_null()
    {
        Assert.Throws<ArgumentNullException>(() => new FolderNodeViewModel(null!, _explorer));
    }
}
