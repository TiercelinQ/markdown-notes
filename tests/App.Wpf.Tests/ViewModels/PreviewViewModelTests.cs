using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using MarkdownNotes.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class PreviewViewModelTests : IDisposable
{
    private readonly EditorViewModel _editor = new(Substitute.For<IMarkdownFormattingService>());
    private readonly IMarkdownRenderService _render = Substitute.For<IMarkdownRenderService>();
    private readonly IWebViewRuntimeService _runtime = Substitute.For<IWebViewRuntimeService>();
    private readonly IFeedbackService _feedback = Substitute.For<IFeedbackService>();
    private readonly List<PreviewViewModel> _created = [];

    public PreviewViewModelTests()
    {
        _runtime.IsAvailable().Returns(true);
        _render.RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<string>.Success("<html>" + call.Arg<string>() + "</html>")));
    }

    public void Dispose()
    {
        foreach (var preview in _created)
        {
            preview.Dispose();
        }
    }

    private PreviewViewModel Create()
    {
        var preview = new PreviewViewModel(
            _editor,
            _render,
            _runtime,
            TestPaths.For(@"C:\data"),
            Substitute.For<ILocalizationService>(),
            _feedback,
            NullLogger<PreviewViewModel>.Instance);
        _created.Add(preview);
        return preview;
    }

    // Activating the preview schedules a debounced background update. These tests call UpdateAsync themselves,
    // so the scheduled one is cancelled right away (Dispose) to keep them deterministic.
    private PreviewViewModel CreateActiveWithoutBackgroundUpdate()
    {
        var preview = Create();
        preview.IsActive = true;
        preview.Dispose();
        _created.Remove(preview);
        return preview;
    }

    [Fact]
    public void Expose_le_dossier_de_donnees_WebView2_et_la_disponibilite_du_runtime()
    {
        var preview = Create();

        Assert.True(preview.IsAvailable);
        Assert.Equal(@"C:\data\WebView2", preview.UserDataFolder);
    }

    [Fact]
    public void Demande_le_defilement_quand_un_titre_est_choisi_et_que_l_apercu_est_actif()
    {
        var preview = Create();
        preview.IsActive = true;

        _editor.NavigateToHeading(2, new TextEdit(0, 0, null, 0, 1));

        Assert.Equal(2, preview.HeadingScrollIndex);
        Assert.Equal(1, preview.HeadingScrollVersion);
    }

    [Fact]
    public void N_envoie_pas_de_defilement_quand_l_apercu_est_masque()
    {
        var preview = Create();

        _editor.NavigateToHeading(2, new TextEdit(0, 0, null, 0, 1));

        Assert.Equal(-1, preview.HeadingScrollIndex);
        Assert.Equal(0, preview.HeadingScrollVersion);
    }

    [Fact]
    public async Task Publie_le_html_rendu_quand_l_apercu_est_actif()
    {
        _editor.Text = "# a";
        var preview = CreateActiveWithoutBackgroundUpdate();

        await preview.UpdateAsync(TestContext.Current.CancellationToken);

        Assert.Equal("<html># a</html>", preview.Html);
    }

    [Fact]
    public async Task Ne_rend_rien_quand_l_apercu_est_inactif()
    {
        var preview = Create();
        _editor.Text = "texte";

        await preview.UpdateAsync(TestContext.Current.CancellationToken);

        await _render.DidNotReceive().RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(string.Empty, preview.Html);
    }

    [Fact]
    public async Task Ne_rend_rien_quand_le_runtime_WebView2_est_absent()
    {
        _runtime.IsAvailable().Returns(false);
        var preview = Create();
        preview.IsActive = true;

        await preview.UpdateAsync(TestContext.Current.CancellationToken);

        Assert.False(preview.IsAvailable);
        await _render.DidNotReceive().RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Affiche_une_seule_fois_un_meme_message_d_erreur_de_rendu()
    {
        var error = new ResultError(FeedbackType.Warning, "Trop gros");
        _render.RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure(error)));
        var preview = CreateActiveWithoutBackgroundUpdate();

        await preview.UpdateAsync(TestContext.Current.CancellationToken);
        await preview.UpdateAsync(TestContext.Current.CancellationToken);

        _feedback.Received(1).Show(error);
    }

    [Fact]
    public async Task Annule_la_mise_a_jour_quand_le_jeton_est_annule()
    {
        var preview = CreateActiveWithoutBackgroundUpdate();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => preview.UpdateAsync(cts.Token));
    }

    [Fact]
    public async Task Regroupe_les_frappes_rapprochees_en_un_seul_rendu_du_dernier_texte()
    {
        _render.RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<string>.Success(call.Arg<string>())));
        var preview = Create();
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        preview.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PreviewViewModel.Html))
            {
                published.TrySetResult();
            }
        };
        preview.IsActive = true;

        _editor.Text = "a";
        _editor.Text = "ab";
        _editor.Text = "abc";
        await published.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        await _render.Received(1).RenderHtmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _render.Received(1).RenderHtmlAsync("abc", Arg.Any<CancellationToken>());
        Assert.Equal("abc", preview.Html);
    }

    [Fact]
    public void Calcule_le_dossier_du_document_depuis_le_chemin_de_l_editeur()
    {
        var preview = Create();
        var changed = new List<string?>();
        preview.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _editor.FilePath = @"C:\docs\sous\note.md";

        Assert.Equal(@"C:\docs\sous", preview.DocumentFolder);
        Assert.Contains(nameof(PreviewViewModel.DocumentFolder), changed);
    }

    [Fact]
    public void Retourne_aucun_dossier_sans_document()
    {
        Assert.Null(Create().DocumentFolder);
    }

    [Fact]
    public void Affiche_un_feedback_danger_quand_WebView2_signale_une_erreur()
    {
        var preview = Create();

        preview.WebViewError = "Echec d'initialisation";

        _feedback.Received(1).Show(Arg.Is<ResultError>(e =>
            e.Type == FeedbackType.Danger && e.Message == Strings.Error_PreviewFailed));
    }

    [Fact]
    public void Ne_notifie_rien_quand_l_erreur_WebView2_est_effacee()
    {
        var preview = Create();
        preview.WebViewError = "Echec";
        _feedback.ClearReceivedCalls();

        preview.WebViewError = null;

        _feedback.DidNotReceive().Show(Arg.Any<ResultError>());
    }
}
