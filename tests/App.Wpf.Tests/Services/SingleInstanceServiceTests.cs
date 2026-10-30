using MarkdownNotes.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarkdownNotes.Tests.Services;

public sealed class SingleInstanceServiceTests
{
    private static SingleInstanceService Create(string key) => new(NullLogger<SingleInstanceService>.Instance, key);

    private static string NewKey() => "test" + Guid.NewGuid().ToString("N");

    [Fact]
    public void Accorde_l_instance_au_premier_lancement_seulement()
    {
        var key = NewKey();
        using var first = Create(key);
        using var second = Create(key);

        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());
    }

    [Fact]
    public void Libere_l_instance_a_la_fermeture_de_la_premiere()
    {
        var key = NewKey();
        var first = Create(key);
        Assert.True(first.TryAcquire());
        first.Dispose();

        using var second = Create(key);

        Assert.True(second.TryAcquire());
    }

    [Fact]
    public async Task Transmet_le_chemin_a_la_premiere_instance()
    {
        var key = NewKey();
        using var first = Create(key);
        using var second = Create(key);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        first.PathReceived += (_, e) => received.TrySetResult(e.Path);
        first.TryAcquire();
        first.StartListening();

        var forwarded = await second.ForwardAsync(@"C:\docs\note.md", TestContext.Current.CancellationToken);

        Assert.True(forwarded);
        Assert.Equal(@"C:\docs\note.md", await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Transmet_une_chaine_vide_quand_aucun_chemin_n_est_fourni()
    {
        var key = NewKey();
        using var first = Create(key);
        using var second = Create(key);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        first.PathReceived += (_, e) => received.TrySetResult(e.Path);
        first.StartListening();

        await second.ForwardAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Retourne_faux_quand_aucune_instance_n_ecoute()
    {
        using var alone = Create(NewKey());

        var forwarded = await alone.ForwardAsync(@"C:\docs\note.md", TestContext.Current.CancellationToken);

        Assert.False(forwarded);
    }
}
