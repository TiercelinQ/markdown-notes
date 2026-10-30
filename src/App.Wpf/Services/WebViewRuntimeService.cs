using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace MarkdownNotes.Services;

/// <summary>Detects the Microsoft Edge WebView2 runtime the preview depends on.</summary>
internal interface IWebViewRuntimeService
{
    /// <summary>Tells whether the WebView2 Evergreen runtime is installed.</summary>
    /// <returns>True when the runtime is available.</returns>
    bool IsAvailable();
}

/// <inheritdoc />
internal sealed class WebViewRuntimeService(ILogger<WebViewRuntimeService> logger) : IWebViewRuntimeService
{
    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            logger.LogWarning(ex, "Runtime WebView2 introuvable");
            return false;
        }
    }
}
