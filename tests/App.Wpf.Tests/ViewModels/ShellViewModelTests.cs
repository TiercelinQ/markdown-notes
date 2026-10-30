using System.Globalization;
using System.Text;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using MarkdownNotes.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class ShellViewModelTests : IDisposable
{
    private const string NotePath = @"C:\docs\note.md";

    private readonly IDocumentService _documents = Substitute.For<IDocumentService>();
    private readonly IFileDialogService _dialogs = Substitute.For<IFileDialogService>();
    private readonly IUnsavedChangesPrompt _prompt = Substitute.For<IUnsavedChangesPrompt>();
    private readonly IRecentFilesService _recent = Substitute.For<IRecentFilesService>();
    private readonly IPreferencesService _preferences = Substitute.For<IPreferencesService>();
    private readonly IAppThemeService _theme = Substitute.For<IAppThemeService>();
    private readonly ILocalizationService _localization = Substitute.For<ILocalizationService>();
    private readonly IFeedbackService _feedback = Substitute.For<IFeedbackService>();
    private readonly IFolderService _folders = Substitute.For<IFolderService>();
    private readonly IWebViewRuntimeService _runtime = Substitute.For<IWebViewRuntimeService>();
    private readonly EditorViewModel _editor = new(Substitute.For<IMarkdownFormattingService>());
    private readonly PreviewViewModel _preview;
    private readonly OutlineViewModel _outline;
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        _runtime.IsAvailable().Returns(true);
        _preferences.Current.Returns(new UserPreferences());
        _preferences.UpdateAsync(Arg.Any<Func<UserPreferences, UserPreferences>>(), Arg.Any<CancellationToken>())
            .Returns(_ => TestPaths.UnitSuccess());
        _recent.AddAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => TestPaths.UnitSuccess());
        _recent.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        _documents.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Encoding>(), Arg.Any<CancellationToken>())
            .Returns(_ => TestPaths.UnitSuccess());
        _theme.Current.Returns(AppTheme.Light);
        _localization.CurrentCulture.Returns("fr");

        _preview = new PreviewViewModel(
            _editor,
            Substitute.For<IMarkdownRenderService>(),
            _runtime,
            TestPaths.For(@"C:\data"),
            _localization,
            _feedback,
            NullLogger<PreviewViewModel>.Instance);
        var explorer = new ExplorerViewModel(_folders, _feedback, _localization);
        _outline = new OutlineViewModel(
            _editor, new OutlineService(), _localization, _feedback, NullLogger<OutlineViewModel>.Instance);
        var find = new FindReplaceViewModel(_editor, new TextSearchService(), _localization);
        _shell = new ShellViewModel(
            _documents, _dialogs, _prompt, _recent, _preferences, _theme, _localization, _feedback,
            _editor, _preview, explorer, _outline, find);
    }

    public void Dispose()
    {
        _preview.Dispose();
        _outline.Dispose();
    }

    private void DocumentReads(string path, string text) =>
        _documents.ReadAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<MarkdownDocument>.Success(new MarkdownDocument(path, text, new UTF8Encoding(false)))));

    private void MakeDirty(string text = "brouillon") => _editor.Text = text;

    private static bool AppliesTo(Func<UserPreferences, UserPreferences> update, Func<UserPreferences, bool> check) =>
        check(update(new UserPreferences()));

    // Open

    [Fact]
    public async Task Ouvre_un_document_et_le_charge_dans_l_editeur()
    {
        DocumentReads(NotePath, "# Bonjour");

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        Assert.Equal("# Bonjour", _editor.Text);
        Assert.Equal(NotePath, _editor.FilePath);
        Assert.False(_editor.IsDirty);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.Status_Opened, "note.md"), _shell.StatusText);
    }

    [Fact]
    public async Task Ajoute_le_document_ouvert_aux_fichiers_recents()
    {
        DocumentReads(NotePath, "x");

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        await _recent.Received(1).AddAsync(NotePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Affiche_une_erreur_et_garde_l_editeur_quand_la_lecture_echoue()
    {
        MakeDirty("en cours");
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Discard);
        _documents.ReadAsync(NotePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<MarkdownDocument>.Failure(new ResultError(FeedbackType.Warning, "Introuvable"))));

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        _feedback.Received(1).Show(Arg.Is<ResultError>(e => e.Message == "Introuvable"));
        Assert.Equal("en cours", _editor.Text);
    }

    [Fact]
    public async Task Ignore_un_chemin_vide_a_l_ouverture()
    {
        await _shell.OpenPathCommand.ExecuteAsync("  ");

        await _documents.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ouvre_le_fichier_choisi_dans_le_dialogue()
    {
        _dialogs.PickOpenFile().Returns(NotePath);
        DocumentReads(NotePath, "x");

        await _shell.OpenCommand.ExecuteAsync(null);

        Assert.Equal(NotePath, _editor.FilePath);
    }

    [Fact]
    public async Task N_ouvre_rien_quand_le_dialogue_est_annule()
    {
        _dialogs.PickOpenFile().Returns((string?)null);

        await _shell.OpenCommand.ExecuteAsync(null);

        await _documents.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Unsaved changes

    [Fact]
    public async Task Annule_l_ouverture_quand_l_utilisateur_refuse_de_perdre_ses_modifications()
    {
        MakeDirty();
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Cancel);

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        await _documents.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal("brouillon", _editor.Text);
    }

    [Fact]
    public async Task Abandonne_les_modifications_puis_ouvre_quand_l_utilisateur_choisit_de_ne_pas_enregistrer()
    {
        MakeDirty();
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Discard);
        DocumentReads(NotePath, "autre");

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        await _documents.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Encoding>(), Arg.Any<CancellationToken>());
        Assert.Equal("autre", _editor.Text);
    }

    [Fact]
    public async Task Enregistre_puis_ouvre_quand_l_utilisateur_choisit_d_enregistrer()
    {
        _editor.MarkSaved(@"C:\docs\ancien.md", string.Empty);
        MakeDirty("a garder");
        _prompt.ConfirmAsync("ancien.md", Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Save);
        DocumentReads(NotePath, "autre");

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        await _documents.Received(1).WriteAsync(@"C:\docs\ancien.md", "a garder", Arg.Any<Encoding>(), Arg.Any<CancellationToken>());
        Assert.Equal("autre", _editor.Text);
    }

    [Fact]
    public async Task N_ouvre_pas_quand_l_enregistrement_demande_echoue()
    {
        _editor.MarkSaved(@"C:\docs\ancien.md", string.Empty);
        MakeDirty();
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Save);
        _documents.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Encoding>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit>.Failure(new ResultError(FeedbackType.Danger, "Disque plein"))));

        await _shell.OpenPathCommand.ExecuteAsync(NotePath);

        await _documents.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // New

    [Fact]
    public async Task Cree_un_nouveau_document_vide()
    {
        _editor.Load(new MarkdownDocument(NotePath, "contenu", new UTF8Encoding(false)));

        await _shell.NewCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, _editor.Text);
        Assert.Null(_editor.FilePath);
    }

    [Fact]
    public async Task Garde_le_document_courant_quand_le_nouveau_document_est_annule()
    {
        MakeDirty();
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Cancel);

        await _shell.NewCommand.ExecuteAsync(null);

        Assert.Equal("brouillon", _editor.Text);
    }

    // Save

    [Fact]
    public async Task Enregistre_dans_le_fichier_courant_sans_demander_de_chemin()
    {
        _editor.Load(new MarkdownDocument(NotePath, "v1", new UTF8Encoding(true)));
        _editor.Text = "v2";

        await _shell.SaveCommand.ExecuteAsync(null);

        _dialogs.DidNotReceive().PickSaveFile(Arg.Any<string>());
        await _documents.Received(1).WriteAsync(NotePath, "v2", _editor.Encoding, Arg.Any<CancellationToken>());
        Assert.False(_editor.IsDirty);
    }

    [Fact]
    public async Task Demande_un_chemin_pour_un_document_sans_fichier()
    {
        MakeDirty("neuf");
        _dialogs.PickSaveFile(Arg.Any<string>()).Returns(@"C:\docs\neuf.md");

        await _shell.SaveCommand.ExecuteAsync(null);

        await _documents.Received(1).WriteAsync(@"C:\docs\neuf.md", "neuf", Arg.Any<Encoding>(), Arg.Any<CancellationToken>());
        Assert.Equal(@"C:\docs\neuf.md", _editor.FilePath);
        Assert.False(_editor.IsDirty);
    }

    [Fact]
    public async Task N_ecrit_rien_quand_le_dialogue_d_enregistrement_est_annule()
    {
        MakeDirty();
        _dialogs.PickSaveFile(Arg.Any<string>()).Returns((string?)null);

        await _shell.SaveCommand.ExecuteAsync(null);

        await _documents.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Encoding>(), Arg.Any<CancellationToken>());
        Assert.True(_editor.IsDirty);
    }

    [Fact]
    public async Task Redemande_toujours_un_chemin_pour_enregistrer_sous()
    {
        _editor.Load(new MarkdownDocument(NotePath, "texte", new UTF8Encoding(false)));
        _dialogs.PickSaveFile(Arg.Any<string>()).Returns(@"C:\docs\copie.md");

        await _shell.SaveAsCommand.ExecuteAsync(null);

        await _documents.Received(1).WriteAsync(@"C:\docs\copie.md", "texte", Arg.Any<Encoding>(), Arg.Any<CancellationToken>());
        Assert.Equal(@"C:\docs\copie.md", _editor.FilePath);
    }

    [Fact]
    public async Task Affiche_une_erreur_et_reste_modifie_quand_l_ecriture_echoue()
    {
        _editor.Load(new MarkdownDocument(NotePath, "v1", new UTF8Encoding(false)));
        _editor.Text = "v2";
        var error = new ResultError(FeedbackType.Danger, "Disque plein");
        _documents.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Encoding>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit>.Failure(error)));

        await _shell.SaveCommand.ExecuteAsync(null);

        _feedback.Received(1).Show(error);
        Assert.True(_editor.IsDirty);
    }

    [Fact]
    public async Task Ajoute_le_fichier_enregistre_aux_fichiers_recents()
    {
        _editor.Load(new MarkdownDocument(NotePath, "v1", new UTF8Encoding(false)));

        await _shell.SaveCommand.ExecuteAsync(null);

        await _recent.Received(1).AddAsync(NotePath, Arg.Any<CancellationToken>());
    }

    // Folder

    [Fact]
    public async Task Ouvre_le_dossier_choisi_et_le_memorise_dans_les_preferences()
    {
        _dialogs.PickFolder().Returns(@"C:\docs");
        _folders.ListChildrenAsync(@"C:\docs", @"C:\docs", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Success([])));
        _shell.ExplorerVisible = false;

        await _shell.OpenFolderCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\docs", _shell.Explorer.RootPath);
        Assert.True(_shell.ExplorerVisible);
        await _preferences.Received(1).UpdateAsync(
            Arg.Is<Func<UserPreferences, UserPreferences>>(u => AppliesTo(u, p => p.LastFolder == @"C:\docs" && p.ExplorerVisible)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ne_memorise_rien_quand_le_dossier_ne_peut_pas_etre_liste()
    {
        _dialogs.PickFolder().Returns(@"C:\docs");
        _folders.ListChildrenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Failure(new ResultError(FeedbackType.Danger, "Echec"))));

        await _shell.OpenFolderCommand.ExecuteAsync(null);

        await _preferences.DidNotReceive().UpdateAsync(Arg.Any<Func<UserPreferences, UserPreferences>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ouvre_le_fichier_active_dans_l_explorateur()
    {
        _folders.ListChildrenAsync(@"C:\docs", @"C:\docs", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FolderNode>>.Success([new FolderNode("note.md", NotePath, false)])));
        DocumentReads(NotePath, "depuis l'explorateur");
        await _shell.Explorer.OpenFolderAsync(@"C:\docs", TestContext.Current.CancellationToken);

        _shell.Explorer.Roots[0].IsSelected = true;

        Assert.Equal("depuis l'explorateur", _editor.Text);
    }

    // Theme, language, view

    [Fact]
    public async Task Applique_le_theme_sombre_et_le_memorise()
    {
        await _shell.SetThemeCommand.ExecuteAsync(AppTheme.Dark);

        _theme.Received(1).Apply(AppTheme.Dark);
        Assert.True(_preview.IsDarkTheme);
        await _preferences.Received(1).UpdateAsync(
            Arg.Is<Func<UserPreferences, UserPreferences>>(u => AppliesTo(u, p => p.Theme == AppTheme.Dark)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Change_de_langue_et_la_memorise()
    {
        _localization.CurrentCulture.Returns("en");

        await _shell.SetLanguageCommand.ExecuteAsync("en");

        _localization.Received(1).SetCulture("en");
        Assert.True(_shell.IsEnglish);
        Assert.False(_shell.IsFrench);
        await _preferences.Received(1).UpdateAsync(
            Arg.Is<Func<UserPreferences, UserPreferences>>(u => AppliesTo(u, p => p.Culture == "en")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ignore_une_langue_vide()
    {
        await _shell.SetLanguageCommand.ExecuteAsync(null);

        _localization.DidNotReceive().SetCulture(Arg.Any<string>());
    }

    [Fact]
    public async Task Desactive_l_apercu_en_mode_editeur_seul_et_le_reactive_ensuite()
    {
        await _shell.SetViewModeCommand.ExecuteAsync(ViewMode.EditorOnly);
        var activeInEditorOnly = _preview.IsActive;
        await _shell.SetViewModeCommand.ExecuteAsync(ViewMode.PreviewOnly);

        Assert.False(activeInEditorOnly);
        Assert.True(_preview.IsActive);
        Assert.True(_shell.IsPreviewOnlyMode);
        Assert.False(_shell.IsEditorOnlyMode);
    }

    [Fact]
    public async Task Bascule_l_explorateur_et_memorise_le_choix()
    {
        await _shell.ToggleExplorerCommand.ExecuteAsync(null);

        Assert.False(_shell.ExplorerVisible);
        await _preferences.Received(1).UpdateAsync(
            Arg.Is<Func<UserPreferences, UserPreferences>>(u => AppliesTo(u, p => !p.ExplorerVisible)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Affiche_une_erreur_quand_les_preferences_ne_peuvent_pas_etre_ecrites()
    {
        var error = new ResultError(FeedbackType.Danger, "Echec");
        _preferences.UpdateAsync(Arg.Any<Func<UserPreferences, UserPreferences>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit>.Failure(error)));

        await _shell.ToggleExplorerCommand.ExecuteAsync(null);

        _feedback.Received(1).Show(error);
    }

    // Find, exit, title

    [Fact]
    public void Ouvre_la_barre_de_recherche_ou_de_remplacement()
    {
        _shell.ShowFindCommand.Execute(null);
        var findOnly = _shell.FindReplace.IsReplaceVisible;
        _shell.ShowReplaceCommand.Execute(null);

        Assert.False(findOnly);
        Assert.True(_shell.FindReplace.IsVisible);
        Assert.True(_shell.FindReplace.IsReplaceVisible);
    }

    [Fact]
    public void Demande_la_fermeture_de_la_fenetre()
    {
        var raised = false;
        _shell.CloseRequested += (_, _) => raised = true;

        _shell.ExitCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void Affiche_le_nom_du_fichier_et_une_etoile_quand_le_document_est_modifie()
    {
        _editor.Load(new MarkdownDocument(NotePath, "v1", new UTF8Encoding(false)));
        var clean = _shell.Title;

        _editor.Text = "v2";

        Assert.Equal("note.md - " + AppConfig.AppDisplayName, clean);
        Assert.Equal("* note.md - " + AppConfig.AppDisplayName, _shell.Title);
        Assert.Equal(Strings.Status_Modified, _shell.ModifiedText);
    }

    [Fact]
    public void Affiche_le_titre_sans_titre_pour_un_nouveau_document()
    {
        Assert.Equal(Strings.Status_Untitled + " - " + AppConfig.AppDisplayName, _shell.Title);
    }

    [Fact]
    public void Affiche_la_position_du_curseur_dans_la_barre_d_etat()
    {
        _editor.Text = "a\nbc";

        _editor.SelectionStart = 3;

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.Status_LineColumn, 2, 2), _shell.CursorText);
    }

    // Initialize, close

    [Fact]
    public async Task Initialise_le_mode_d_affichage_et_l_explorateur_depuis_les_preferences()
    {
        _preferences.Current.Returns(new UserPreferences { ViewMode = ViewMode.EditorOnly, ExplorerVisible = false });

        await _shell.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.True(_shell.IsEditorOnlyMode);
        Assert.False(_shell.ExplorerVisible);
        Assert.False(_preview.IsActive);
    }

    [Fact]
    public async Task Affiche_l_erreur_de_demarrage_et_l_avertissement_WebView2_manquant()
    {
        _runtime.IsAvailable().Returns(false);
        using var unavailable = new PreviewViewModel(
            _editor, Substitute.For<IMarkdownRenderService>(), _runtime, TestPaths.For(@"C:\data"),
            _localization, _feedback, NullLogger<PreviewViewModel>.Instance);
        var shell = new ShellViewModel(
            _documents, _dialogs, _prompt, _recent, _preferences, _theme, _localization, _feedback,
            _editor, unavailable, new ExplorerViewModel(_folders, _feedback, _localization), _outline,
            new FindReplaceViewModel(_editor, new TextSearchService(), _localization))
        {
            StartupError = new ResultError(FeedbackType.Warning, "Argument refuse"),
        };

        await shell.InitializeAsync(TestContext.Current.CancellationToken);

        _feedback.Received(1).Show(Arg.Is<ResultError>(e => e.Message == "Argument refuse"));
        _feedback.Received(1).Show(Arg.Is<ResultError>(e => e.Message == Strings.Warning_WebViewMissing));
    }

    [Fact]
    public async Task Ouvre_le_document_passe_en_argument_au_demarrage()
    {
        DocumentReads(NotePath, "argument");
        _shell.StartupPath = NotePath;

        await _shell.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("argument", _editor.Text);
    }

    [Fact]
    public async Task Refuse_la_fermeture_quand_l_utilisateur_annule_et_n_ecrit_pas_les_preferences()
    {
        MakeDirty();
        _prompt.ConfirmAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UnsavedChoice.Cancel);

        var closed = await _shell.TryCloseAsync(10, 20, 800, 600, false, TestContext.Current.CancellationToken);

        Assert.False(closed);
        await _preferences.DidNotReceive().UpdateAsync(Arg.Any<Func<UserPreferences, UserPreferences>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Memorise_la_position_de_la_fenetre_a_la_fermeture()
    {
        var closed = await _shell.TryCloseAsync(10, 20, 800, 600, true, TestContext.Current.CancellationToken);

        Assert.True(closed);
        await _preferences.Received(1).UpdateAsync(
            Arg.Is<Func<UserPreferences, UserPreferences>>(u => AppliesTo(u, p =>
                p.WindowLeft == 10 && p.WindowTop == 20 && p.WindowWidth == 800 && p.WindowHeight == 600 && p.WindowMaximized)),
            Arg.Any<CancellationToken>());
    }
}
