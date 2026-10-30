using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using MarkdownNotes.Services;
using MarkdownNotes.Tests.Support;
using NSubstitute;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class FeedbackServiceTests
{
    private readonly ISnackbarService _snackbar = Substitute.For<ISnackbarService>();

    private void Show(ResultError error)
    {
        var service = new FeedbackService(_snackbar);
        StaThread.Run(() => service.Show(error));
    }

    [Theory]
    [InlineData(nameof(FeedbackType.Success), ControlAppearance.Success, 4000d)]
    [InlineData(nameof(FeedbackType.Info), ControlAppearance.Info, 4000d)]
    [InlineData(nameof(FeedbackType.Warning), ControlAppearance.Caution, 6000d)]
    [InlineData(nameof(FeedbackType.Danger), ControlAppearance.Danger, int.MaxValue)]
    // The type travels as text: FeedbackType is internal and a public test method cannot expose it.
    public void Affiche_un_snackbar_avec_l_apparence_et_la_duree_du_type(string type, ControlAppearance appearance, double timeoutMs)
    {
        Show(new ResultError(Enum.Parse<FeedbackType>(type), "Message"));

        _snackbar.Received(1).Show(
            Arg.Any<string>(),
            "Message",
            appearance,
            Arg.Any<IconElement>(),
            TimeSpan.FromMilliseconds(timeoutMs));
    }

    [Fact]
    public void Utilise_le_titre_localise_du_type()
    {
        Show(new ResultError(FeedbackType.Danger, "Message"));

        _snackbar.Received(1).Show(
            Strings.Feedback_Danger,
            Arg.Any<string>(),
            Arg.Any<ControlAppearance>(),
            Arg.Any<IconElement>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public void Ajoute_la_description_sur_une_seconde_ligne()
    {
        Show(new ResultError(FeedbackType.Warning, "Message", "Detail"));

        _snackbar.Received(1).Show(
            Arg.Any<string>(),
            "Message\nDetail",
            Arg.Any<ControlAppearance>(),
            Arg.Any<IconElement>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public void Refuse_une_erreur_null()
    {
        var service = new FeedbackService(_snackbar);

        Assert.Throws<ArgumentNullException>(() => service.Show(null!));
    }
}
