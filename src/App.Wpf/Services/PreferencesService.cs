using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarkdownNotes.Models;
using MarkdownNotes.Resources;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.Services;

/// <summary>Loads and persists the user preferences.</summary>
internal interface IPreferencesService
{
    /// <summary>Gets the preferences currently in effect.</summary>
    UserPreferences Current { get; }

    /// <summary>Loads <c>preferences.json</c>; a missing file keeps the defaults.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a warning when the file is unreadable (defaults are used).</returns>
    Task<Result<Unit>> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Applies a change to the preferences and writes them to disk.</summary>
    /// <param name="update">Function producing the new preferences from the current ones.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a danger when the file cannot be written (the change stays in memory).</returns>
    Task<Result<Unit>> UpdateAsync(Func<UserPreferences, UserPreferences> update, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class PreferencesService(IAppPaths paths, ILogger<PreferencesService> logger) : IPreferencesService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public UserPreferences Current { get; private set; } = new();

    /// <inheritdoc />
    public async Task<Result<Unit>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(paths.PreferencesFile))
            {
                return Result<Unit>.Success(Unit.Value);
            }

            try
            {
                await using var stream = File.OpenRead(paths.PreferencesFile);
                var loaded = await JsonSerializer.DeserializeAsync<UserPreferences>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                Current = Sanitize(loaded ?? new UserPreferences());
                return Result<Unit>.Success(Unit.Value);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Preferences illisibles, valeurs par defaut utilisees");
                Current = new UserPreferences();
                return Result<Unit>.Failure(new ResultError(FeedbackType.Warning, Strings.Error_PreferencesLoad));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> UpdateAsync(Func<UserPreferences, UserPreferences> update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Current = Sanitize(update(Current));

            try
            {
                Directory.CreateDirectory(paths.UserData);
                var temporary = paths.PreferencesFile + ".tmp";
                await using (var stream = File.Create(temporary))
                {
                    await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, cancellationToken).ConfigureAwait(false);
                }

                File.Move(temporary, paths.PreferencesFile, overwrite: true);
                return Result<Unit>.Success(Unit.Value);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogError(ex, "Echec de l'ecriture des preferences");
                return Result<Unit>.Failure(new ResultError(FeedbackType.Danger, Strings.Error_PreferencesSave));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private static UserPreferences Sanitize(UserPreferences preferences)
    {
        var culture = AppConfig.SupportedCultures.Contains(preferences.Culture, StringComparer.OrdinalIgnoreCase)
            ? preferences.Culture.ToLowerInvariant()
            : AppConfig.DefaultCulture;

        return preferences with
        {
            Culture = culture,
            ViewMode = Enum.IsDefined(preferences.ViewMode) ? preferences.ViewMode : ViewMode.Split,
            RecentFiles = (preferences.RecentFiles ?? []).Where(path => !string.IsNullOrWhiteSpace(path))
                .Take(AppConfig.RecentFilesMax).ToList(),
            WindowLeft = FiniteOrNull(preferences.WindowLeft),
            WindowTop = FiniteOrNull(preferences.WindowTop),
            WindowWidth = PositiveOrNull(preferences.WindowWidth),
            WindowHeight = PositiveOrNull(preferences.WindowHeight),
        };
    }

    private static double? FiniteOrNull(double? value) => value is { } v && double.IsFinite(v) ? v : null;

    private static double? PositiveOrNull(double? value) => value is { } v && double.IsFinite(v) && v > 0 ? v : null;
}
