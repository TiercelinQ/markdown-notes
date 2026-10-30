using System.Windows;
using MarkdownNotes.Models;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MarkdownNotes.Services;

/// <summary>Applies the theme: Fluent dictionary, project accent and the matching token dictionary.</summary>
internal interface IAppThemeService
{
    /// <summary>Gets the theme currently applied.</summary>
    AppTheme Current { get; }

    /// <summary>Resolves the theme to use at startup.</summary>
    /// <param name="stored">The persisted choice, or null to follow Windows.</param>
    /// <returns>The theme to apply.</returns>
    AppTheme ResolveInitial(AppTheme? stored);

    /// <summary>Swaps the theme and re-applies the accent (its variants differ per theme).</summary>
    /// <param name="theme">The theme to apply.</param>
    void Apply(AppTheme theme);
}

/// <inheritdoc />
internal sealed class AppThemeService : IAppThemeService
{
    private const string LightTokens = "Themes/Tokens.Light.xaml";
    private const string DarkTokens = "Themes/Tokens.Dark.xaml";

    /// <inheritdoc />
    public AppTheme Current { get; private set; } = AppTheme.Light;

    /// <inheritdoc />
    public AppTheme ResolveInitial(AppTheme? stored) =>
        stored ?? (WindowsUsesLightApps() ? AppTheme.Light : AppTheme.Dark);

    /// <inheritdoc />
    public void Apply(AppTheme theme)
    {
        var wpfTheme = theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        // Theme first (swaps the Fluent dictionary), accent second so it owns the Accent* resources.
        ApplicationThemeManager.Apply(wpfTheme, WindowBackdropType.Mica, updateAccent: false);
        ApplicationAccentColorManager.Apply(AppConfig.AccentColorValue, wpfTheme);
        ReplaceTokenDictionary(theme);
        Current = theme;
    }

    private static bool WindowsUsesLightApps() =>
        Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme",
            1) is not int value || value != 0;

    private static void ReplaceTokenDictionary(AppTheme theme)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary
        {
            Source = new Uri(theme == AppTheme.Dark ? DarkTokens : LightTokens, UriKind.Relative),
        };

        for (var i = 0; i < merged.Count; i++)
        {
            var source = merged[i].Source?.OriginalString;
            if (source is not null
                && (source.EndsWith("Tokens.Light.xaml", StringComparison.OrdinalIgnoreCase)
                    || source.EndsWith("Tokens.Dark.xaml", StringComparison.OrdinalIgnoreCase)))
            {
                merged[i] = replacement;
                return;
            }
        }

        merged.Add(replacement);
    }
}
