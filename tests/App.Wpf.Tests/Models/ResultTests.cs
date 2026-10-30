using MarkdownNotes.Models;
using Xunit;

namespace MarkdownNotes.Tests.Models;

public sealed class ResultTests
{
    [Fact]
    public void Un_succes_porte_les_donnees_et_aucune_erreur()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.Ok);
        Assert.Equal(42, result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Un_echec_porte_l_erreur_et_aucune_donnee()
    {
        var error = new ResultError(FeedbackType.Danger, "Echec", "Detail");

        var result = Result<string>.Failure(error);

        Assert.False(result.Ok);
        Assert.Equal(error, result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Un_resultat_par_defaut_n_est_pas_un_succes()
    {
        var result = default(Result<int>);

        Assert.False(result.Ok);
    }

    [Fact]
    public void Unit_expose_une_instance_unique()
    {
        Assert.Same(Unit.Value, Unit.Value);
    }
}
