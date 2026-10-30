using System.IO;
using MarkdownNotes.Models;
using MarkdownNotes.Services;
using NSubstitute;

namespace MarkdownNotes.Tests.Support;

/// <summary>Builders for the doubles shared by several test classes.</summary>
internal static class TestPaths
{
    public static IAppPaths For(string userData)
    {
        var paths = Substitute.For<IAppPaths>();
        paths.UserData.Returns(userData);
        paths.LogsDirectory.Returns(Path.Combine(userData, "logs"));
        paths.PreferencesFile.Returns(Path.Combine(userData, AppConfig.PreferencesFileName));
        paths.WebViewDataFolder.Returns(Path.Combine(userData, "WebView2"));
        return paths;
    }

    public static Task<Result<Unit>> UnitSuccess() => Task.FromResult(Result<Unit>.Success(Unit.Value));
}
