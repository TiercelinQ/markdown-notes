using System.Windows;
using System.Windows.Controls;
using MarkdownNotes.Tests.Support;
using MarkdownNotes.Views.Behaviors;
using Xunit;

namespace MarkdownNotes.Tests.Behaviors;

public sealed class PaneLayoutBehaviorTests
{
    [Fact]
    public void Retrouve_la_largeur_tiree_apres_fermeture_de_la_colonne()
    {
        StaThread.Run(() =>
        {
            var column = new ColumnDefinition();
            PaneColumnBehavior.SetOpenWidth(column, 280);
            PaneColumnBehavior.SetMinOpenWidth(column, 160);
            PaneColumnBehavior.SetIsOpen(column, true);
            column.Width = new GridLength(420);

            PaneColumnBehavior.SetIsOpen(column, false);

            Assert.Equal(0, column.Width.Value);
            Assert.Equal(0, column.MaxWidth);

            PaneColumnBehavior.SetIsOpen(column, true);

            Assert.Equal(420, column.Width.Value);
            Assert.Equal(160, column.MinWidth);
        });
    }

    [Fact]
    public void Donne_la_place_au_volet_ouvert_puis_restaure_le_partage()
    {
        StaThread.Run(() =>
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300) });
            SplitPaneBehavior.SetFirstOpen(grid, true);
            SplitPaneBehavior.SetSecondOpen(grid, true);

            SplitPaneBehavior.SetFirstOpen(grid, false);

            Assert.Equal(GridLength.Auto, grid.RowDefinitions[0].Height);
            Assert.Equal(GridUnitType.Star, grid.RowDefinitions[2].Height.GridUnitType);

            SplitPaneBehavior.SetFirstOpen(grid, true);

            Assert.Equal(200, grid.RowDefinitions[0].Height.Value);
            Assert.Equal(300, grid.RowDefinitions[2].Height.Value);
        });
    }
}
