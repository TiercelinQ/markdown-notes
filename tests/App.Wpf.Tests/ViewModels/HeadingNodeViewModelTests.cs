using MarkdownNotes.Models;
using MarkdownNotes.Services;
using MarkdownNotes.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MarkdownNotes.Tests.ViewModels;

public sealed class HeadingNodeViewModelTests : IDisposable
{
    private readonly EditorViewModel _editor = new(Substitute.For<IMarkdownFormattingService>());
    private readonly OutlineViewModel _outline;

    public HeadingNodeViewModelTests() =>
        _outline = new OutlineViewModel(
            _editor,
            new OutlineService(),
            Substitute.For<ILocalizationService>(),
            Substitute.For<IFeedbackService>(),
            NullLogger<OutlineViewModel>.Instance);

    public void Dispose() => _outline.Dispose();

    [Fact]
    public void Reprend_le_texte_la_position_et_les_enfants_du_titre()
    {
        var heading = new HeadingNode("A", 1, 4, 3);
        heading.AddChild(new HeadingNode("A1", 2, 10, 5));

        var node = new HeadingNodeViewModel(heading, _outline);

        Assert.Equal("A", node.Text);
        Assert.Equal(4, node.Offset);
        Assert.Equal(3, node.Length);
        Assert.Equal("A1", Assert.Single(node.Children).Text);
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public async Task Deplace_l_editeur_puis_efface_sa_selection_quand_il_est_selectionne()
    {
        var node = new HeadingNodeViewModel(new HeadingNode("A", 1, 4, 3), _outline);

        node.IsSelected = true;

        var edit = _editor.PendingEdit;
        Assert.NotNull(edit);
        Assert.Equal(4, edit.SelectionStart);
        Assert.Equal(3, edit.SelectionLength);
        await WaitForAsync(() => !node.IsSelected);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }
}
