using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownNotes.Resources;

namespace MarkdownNotes.Services;

/// <summary>Observable access to the localized strings: XAML binds <c>Loc[Key]</c> and refreshes on a culture change.</summary>
internal interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>Gets the string for a resource key in the current culture; the key itself when missing.</summary>
    /// <param name="key">Resource key.</param>
    /// <returns>The localized string.</returns>
    string this[string key] { get; }

    /// <summary>Gets the current culture code (<c>fr</c> or <c>en</c>).</summary>
    string CurrentCulture { get; }

    /// <summary>Switches the UI culture and refreshes every bound string.</summary>
    /// <param name="culture">Culture code; an unsupported value falls back to the default.</param>
    void SetCulture(string culture);
}

/// <inheritdoc cref="ILocalizationService" />
internal sealed class LocalizationService : ObservableObject, ILocalizationService
{
    private string _currentCulture = AppConfig.DefaultCulture;

    /// <inheritdoc />
    public string CurrentCulture => _currentCulture;

    /// <inheritdoc />
    public string this[string key] => Strings.ResourceManager.GetString(key, Strings.Culture) ?? key;

    /// <inheritdoc />
    public void SetCulture(string culture)
    {
        var code = AppConfig.SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase)
            ? culture.ToLowerInvariant()
            : AppConfig.DefaultCulture;
        var info = CultureInfo.GetCultureInfo(code);

        Strings.Culture = info;
        CultureInfo.DefaultThreadCurrentUICulture = info;
        CultureInfo.CurrentUICulture = info;
        _currentCulture = code;

        OnPropertyChanged(nameof(CurrentCulture));
        OnPropertyChanged("Item[]");
    }
}
