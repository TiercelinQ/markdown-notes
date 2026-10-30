using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MarkdownNotes.Views.Behaviors;

/// <summary>
/// Drives a WebView2 control from bound properties: user data folder, rendered HTML, document folder
/// (relative images), color scheme. The page shell is navigated once; every later update swaps the body
/// so the scroll position is kept. Navigation started by the user is cancelled, links stay inert.
/// </summary>
internal static class WebViewBehavior
{
    /// <summary>Hooks the control life cycle; set to true once in XAML.</summary>
    public static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
        "IsAttached", typeof(bool), typeof(WebViewBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

    /// <summary>User data folder, applied before the control initializes.</summary>
    public static readonly DependencyProperty UserDataFolderProperty = DependencyProperty.RegisterAttached(
        "UserDataFolder", typeof(string), typeof(WebViewBehavior), new PropertyMetadata(null, OnUserDataFolderChanged));

    /// <summary>Full HTML page produced by the render service.</summary>
    public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
        "Html", typeof(string), typeof(WebViewBehavior), new PropertyMetadata(string.Empty, OnHtmlChanged));

    /// <summary>Folder of the current document, mapped to the virtual host for relative images.</summary>
    public static readonly DependencyProperty DocumentFolderProperty = DependencyProperty.RegisterAttached(
        "DocumentFolder", typeof(string), typeof(WebViewBehavior), new PropertyMetadata(null, OnDocumentFolderChanged));

    /// <summary>Whether the application is in dark mode (drives the preferred color scheme of the page).</summary>
    public static readonly DependencyProperty IsDarkThemeProperty = DependencyProperty.RegisterAttached(
        "IsDarkTheme", typeof(bool), typeof(WebViewBehavior), new PropertyMetadata(false, OnIsDarkThemeChanged));

    /// <summary>Description of a WebView2 failure; bind one way to source.</summary>
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
        "Error", typeof(string), typeof(WebViewBehavior), new PropertyMetadata(null));

    /// <summary>Document index of the heading the preview should scroll to.</summary>
    public static readonly DependencyProperty HeadingScrollIndexProperty = DependencyProperty.RegisterAttached(
        "HeadingScrollIndex", typeof(int), typeof(WebViewBehavior), new PropertyMetadata(-1));

    /// <summary>Changes on every outline click so a second click on the same heading scrolls again.</summary>
    public static readonly DependencyProperty HeadingScrollVersionProperty = DependencyProperty.RegisterAttached(
        "HeadingScrollVersion", typeof(int), typeof(WebViewBehavior), new PropertyMetadata(0, OnHeadingScrollVersionChanged));

    private const string MermaidResource = "MarkdownNotes.Assets.mermaid.min.js";

    // Markdig renders a ```mermaid fence as <pre class="mermaid">.
    private const string MermaidMarker = "class=\"mermaid\"";

    private static readonly Lazy<string> MermaidScript = new(LoadMermaidScript);

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(WebViewState), typeof(WebViewBehavior), new PropertyMetadata(null));

    /// <summary>Gets <see cref="IsAttachedProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static bool GetIsAttached(DependencyObject element) => (bool)element.GetValue(IsAttachedProperty);

    /// <summary>Sets <see cref="IsAttachedProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetIsAttached(DependencyObject element, bool value) => element.SetValue(IsAttachedProperty, value);

    /// <summary>Gets <see cref="UserDataFolderProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static string? GetUserDataFolder(DependencyObject element) => (string?)element.GetValue(UserDataFolderProperty);

    /// <summary>Sets <see cref="UserDataFolderProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetUserDataFolder(DependencyObject element, string? value) => element.SetValue(UserDataFolderProperty, value);

    /// <summary>Gets <see cref="HtmlProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static string GetHtml(DependencyObject element) => (string)element.GetValue(HtmlProperty);

    /// <summary>Sets <see cref="HtmlProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetHtml(DependencyObject element, string value) => element.SetValue(HtmlProperty, value);

    /// <summary>Gets <see cref="DocumentFolderProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static string? GetDocumentFolder(DependencyObject element) => (string?)element.GetValue(DocumentFolderProperty);

    /// <summary>Sets <see cref="DocumentFolderProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetDocumentFolder(DependencyObject element, string? value) => element.SetValue(DocumentFolderProperty, value);

    /// <summary>Gets <see cref="IsDarkThemeProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static bool GetIsDarkTheme(DependencyObject element) => (bool)element.GetValue(IsDarkThemeProperty);

    /// <summary>Sets <see cref="IsDarkThemeProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetIsDarkTheme(DependencyObject element, bool value) => element.SetValue(IsDarkThemeProperty, value);

    /// <summary>Gets <see cref="ErrorProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static string? GetError(DependencyObject element) => (string?)element.GetValue(ErrorProperty);

    /// <summary>Sets <see cref="ErrorProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetError(DependencyObject element, string? value) => element.SetValue(ErrorProperty, value);

    /// <summary>Gets <see cref="HeadingScrollIndexProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static int GetHeadingScrollIndex(DependencyObject element) => (int)element.GetValue(HeadingScrollIndexProperty);

    /// <summary>Sets <see cref="HeadingScrollIndexProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetHeadingScrollIndex(DependencyObject element, int value) => element.SetValue(HeadingScrollIndexProperty, value);

    /// <summary>Gets <see cref="HeadingScrollVersionProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static int GetHeadingScrollVersion(DependencyObject element) => (int)element.GetValue(HeadingScrollVersionProperty);

    /// <summary>Sets <see cref="HeadingScrollVersionProperty"/>.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetHeadingScrollVersion(DependencyObject element, int value) => element.SetValue(HeadingScrollVersionProperty, value);

    private static WebViewState StateOf(WebView2CompositionControl webView)
    {
        if (webView.GetValue(StateProperty) is not WebViewState state)
        {
            state = new WebViewState();
            webView.SetValue(StateProperty, state);
        }

        return state;
    }

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2CompositionControl webView || e.NewValue is not true)
        {
            return;
        }

        webView.CoreWebView2InitializationCompleted += OnInitializationCompleted;
        webView.Loaded += OnLoaded;
        webView.SizeChanged += (_, _) => SyncBounds(webView);
        webView.Unloaded += OnUnloaded;
    }

    private static void OnUserDataFolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView2CompositionControl webView && e.NewValue is string folder && !string.IsNullOrWhiteSpace(folder))
        {
            webView.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = folder };
        }
    }

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2CompositionControl webView || e.NewValue is not string html)
        {
            return;
        }

        var state = StateOf(webView);
        var start = html.IndexOf(AppConfig.PreviewBodyStart, StringComparison.Ordinal);
        var end = html.LastIndexOf(AppConfig.PreviewBodyEnd, StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            return;
        }

        var bodyStart = start + AppConfig.PreviewBodyStart.Length;
        var shell = string.Concat(html.AsSpan(0, bodyStart), html.AsSpan(end));
        state.Body = html[bodyStart..end];

        if (!string.Equals(shell, state.Shell, StringComparison.Ordinal))
        {
            state.Shell = shell;
            state.ShellLoaded = false;
            NavigateShell(webView, state);
        }
        else if (state.ShellLoaded)
        {
            _ = InjectBodyAsync(webView, state);
        }
    }

    private static void OnDocumentFolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView2CompositionControl { CoreWebView2: not null } webView)
        {
            ApplyFolderMapping(webView, e.NewValue as string);
        }
    }

    private static void OnIsDarkThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView2CompositionControl { CoreWebView2: not null } webView)
        {
            ApplyColorScheme(webView);

            // Diagrams are drawn with a theme: draw them again.
            var state = StateOf(webView);
            if (state.ShellLoaded)
            {
                _ = InjectBodyAsync(webView, state);
            }
        }
    }

    private static async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var webView = (WebView2CompositionControl)sender;
        try
        {
            await webView.EnsureCoreWebView2Async();
            StretchSurface(webView);
            HookWindow(webView);
            SyncBounds(webView);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            // The view model already reports a missing runtime and hides this control.
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ArgumentException)
        {
            webView.SetCurrentValue(ErrorProperty, ex.Message);
        }
    }

    private static void OnInitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        var webView = (WebView2CompositionControl)sender!;
        if (!e.IsSuccess)
        {
            webView.SetCurrentValue(ErrorProperty, e.InitializationException?.Message ?? "WebView2");
            return;
        }

        var core = webView.CoreWebView2;
        ConfigureSettings(core);
        core.NavigationStarting += OnNavigationStarting;
        core.NewWindowRequested += OnNewWindowRequested;
        core.NavigationCompleted += (_, args) => OnNavigationCompleted(webView, args);

        ApplyColorScheme(webView);
        ApplyFolderMapping(webView, GetDocumentFolder(webView));
        NavigateShell(webView, StateOf(webView));
    }

    private static void ConfigureSettings(CoreWebView2 core)
    {
        var settings = core.Settings;
        // Required by mermaid (DOMPurify's Trusted Types callback fails when scripting is off).
        // Page scripts stay impossible: the CSP is default-src 'none' and raw HTML is disabled in the pipeline,
        // so only the scripts injected by this behavior run.
        settings.IsScriptEnabled = true;
        settings.IsWebMessageEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsSwipeNavigationEnabled = false;
#if DEBUG
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
        settings.AreBrowserAcceleratorKeysEnabled = true;
#else
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
    }

    private static void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // A link to a heading of the same document scrolls the page; nothing is navigated.
        if (e.IsUserInitiated && sender is CoreWebView2 core && TryGetAnchor(e.Uri, out var id))
        {
            e.Cancel = true;
            _ = ScrollToAnchorAsync(core, id);
            return;
        }

        // Otherwise only the programmatic shell navigation is allowed; links and redirects never leave the page.
        if (e.IsUserInitiated || !e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
        }
    }

    private static void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;

    private static void OnNavigationCompleted(WebView2CompositionControl webView, CoreWebView2NavigationCompletedEventArgs e)
    {
        var state = StateOf(webView);
        state.PendingNavigations = Math.Max(0, state.PendingNavigations - 1);
        if (!e.IsSuccess)
        {
            // A shell navigation replaced by a newer one still pending completes as aborted: not an error.
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled && state.PendingNavigations == 0)
            {
                webView.SetCurrentValue(ErrorProperty, e.WebErrorStatus.ToString());
            }

            return;
        }

        state.ShellLoaded = true;
        _ = InjectBodyAsync(webView, state);
    }

    private static void NavigateShell(WebView2CompositionControl webView, WebViewState state)
    {
        if (webView.CoreWebView2 is { } core && state.Shell is not null)
        {
            state.MermaidLoad = null;
            state.PendingNavigations++;
            core.NavigateToString(state.Shell);
        }
    }

    private static async Task InjectBodyAsync(WebView2CompositionControl webView, WebViewState state)
    {
        if (state.Body is null || webView.CoreWebView2 is null)
        {
            return;
        }

        try
        {
            var hasDiagram = state.Body.Contains(MermaidMarker, StringComparison.Ordinal);
            if (hasDiagram)
            {
                // Page scripts stay disabled (and the CSP closed): the library is run by the host, once per shell.
                await (state.MermaidLoad ??= webView.ExecuteScriptAsync(MermaidScript.Value));
            }

            var script = "document.body.innerHTML = " + JsonSerializer.Serialize(state.Body) + ";";
            if (hasDiagram)
            {
                script += MermaidRunScript(GetIsDarkTheme(webView));
            }

            await webView.ExecuteScriptAsync(script);
            await ScrollToHeadingAsync(webView, state);
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ObjectDisposedException)
        {
            state.MermaidLoad = null;
            webView.SetCurrentValue(ErrorProperty, ex.Message);
        }
    }

    // Strict level: no click handlers, labels sanitized. Mermaid empties a block it cannot parse, so the source text is
    // put back for the blocks of this run that did not get a diagram (a newer body swap detaches them: left alone).
    private static string MermaidRunScript(bool dark) =>
        "mermaid.initialize({ startOnLoad: false, securityLevel: 'strict', suppressErrorRendering: true, theme: '" + (dark ? "dark" : "default") + "' });"
        + "(function () {"
        + "var nodes = Array.from(document.querySelectorAll('.mermaid'));"
        + "var sources = nodes.map(function (n) { return n.textContent; });"
        + "mermaid.run({ nodes: nodes, suppressErrors: true }).then(function () {"
        + "nodes.forEach(function (n, i) { if (n.isConnected && !n.querySelector('svg')) { n.textContent = sources[i]; } });"
        + "});"
        + "})();";

    private static string LoadMermaidScript()
    {
        using var stream = typeof(WebViewBehavior).Assembly.GetManifestResourceStream(MermaidResource)
            ?? throw new InvalidOperationException($"Ressource embarquee introuvable: {MermaidResource}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static bool TryGetAnchor(string uri, out string id)
    {
        // The page has a <base> on the virtual host, so a "#section" link resolves to "https://host/#section".
        var prefix = "https://" + AppConfig.PreviewVirtualHost + "/#";
        if (uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && uri.Length > prefix.Length)
        {
            id = Uri.UnescapeDataString(uri[prefix.Length..]);
            return true;
        }

        id = string.Empty;
        return false;
    }

    private static async Task ScrollToAnchorAsync(CoreWebView2 core, string id)
    {
        try
        {
            await core.ExecuteScriptAsync("document.getElementById(" + JsonSerializer.Serialize(id) + ")?.scrollIntoView();");
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ObjectDisposedException)
        {
            // The page was replaced while the script was in flight: nothing to scroll.
        }
    }

    private static void ApplyColorScheme(WebView2CompositionControl webView)
    {
        webView.CoreWebView2.Profile.PreferredColorScheme = GetIsDarkTheme(webView)
            ? CoreWebView2PreferredColorScheme.Dark
            : CoreWebView2PreferredColorScheme.Light;
    }

    private static void ApplyFolderMapping(WebView2CompositionControl webView, string? folder)
    {
        var core = webView.CoreWebView2;
        core.ClearVirtualHostNameToFolderMapping(AppConfig.PreviewVirtualHost);
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
        {
            core.SetVirtualHostNameToFolderMapping(AppConfig.PreviewVirtualHost, folder, CoreWebView2HostResourceAccessKind.DenyCors);
        }
    }

    private sealed class WebViewState
    {
        public string? Shell { get; set; }

        public string? Body { get; set; }

        public bool ShellLoaded { get; set; }

        public int PendingNavigations { get; set; }

        /// <summary>Gets or sets the pending or finished injection of the diagram library into the current shell.</summary>
        public Task? MermaidLoad { get; set; }

        public int PendingHeading { get; set; } = -1;

        public CoreWebView2Controller? Controller { get; set; }

        public bool Syncing { get; set; }

        public Window? Window { get; set; }

        public SizeChangedEventHandler? WindowSizeChanged { get; set; }

        public EventHandler? WindowStateChanged { get; set; }
    }

    private static void OnHeadingScrollVersionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2CompositionControl webView || e.NewValue is not int version || version == 0)
        {
            return;
        }

        var index = GetHeadingScrollIndex(webView);
        if (index < 0)
        {
            return;
        }

        var state = StateOf(webView);
        state.PendingHeading = index;
        _ = ScrollToHeadingAsync(webView, state);
    }

    private static async Task ScrollToHeadingAsync(WebView2CompositionControl webView, WebViewState state)
    {
        if (state.PendingHeading < 0 || webView.CoreWebView2 is null || !state.ShellLoaded)
        {
            return;
        }

        var index = state.PendingHeading;
        try
        {
            var found = await webView.ExecuteScriptAsync(
                "(function(){var h=document.querySelectorAll('h1,h2,h3,h4,h5,h6')["
                + index.ToString(CultureInfo.InvariantCulture)
                + "];if(!h)return false;h.scrollIntoView({block:'start'});return true;})()");
            if (found == "true" && state.PendingHeading == index)
            {
                state.PendingHeading = -1;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ObjectDisposedException)
        {
            // The page was replaced while the script was in flight: the next body injection scrolls again.
        }
    }

    // The composition surface keeps the size it had before a maximize: WindowChrome does not
    // push the new client size into the controller. The controller is not public, so it is reached
    // from the control and its bounds are set from the arranged size.
    private static void SyncBounds(WebView2CompositionControl webView)
    {
        var state = StateOf(webView);
        if (state.Syncing || !webView.IsLoaded)
        {
            return;
        }

        state.Syncing = true;
        try
        {
            var width = webView.ActualWidth;
            var height = webView.ActualHeight;
            if (webView.Parent is FrameworkElement host && host.ActualWidth >= 1 && host.ActualHeight >= 1)
            {
                width = host.ActualWidth;
                height = host.ActualHeight;
                if (double.IsNaN(webView.Width) || Math.Abs(webView.Width - width) > 0.5)
                {
                    webView.Width = width;
                }

                if (double.IsNaN(webView.Height) || Math.Abs(webView.Height - height) > 0.5)
                {
                    webView.Height = height;
                }
            }

            if (width < 1 || height < 1)
            {
                return;
            }

            var controller = ControllerOf(webView, state);
            if (controller is null)
            {
                return;
            }

            var bounds = BoundsFor(webView, controller, width, height);
            if (controller.Bounds == bounds)
            {
                return;
            }

            controller.Bounds = bounds;
            controller.NotifyParentWindowPositionChanged();
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or ArgumentException)
        {
            // The controller was closed while the window was resizing.
        }
        finally
        {
            state.Syncing = false;
        }
    }

    private static System.Drawing.Rectangle BoundsFor(
        WebView2CompositionControl webView, CoreWebView2Controller controller, double width, double height)
    {
        if (controller.BoundsMode == CoreWebView2BoundsMode.UseRawPixels)
        {
            var scale = controller.RasterizationScale > 0
                ? controller.RasterizationScale
                : VisualTreeHelper.GetDpi(webView).DpiScaleX;
            width *= scale;
            height *= scale;
        }

        return new System.Drawing.Rectangle(0, 0, Math.Max(0, (int)Math.Round(width)), Math.Max(0, (int)Math.Round(height)));
    }

    private static CoreWebView2Controller? ControllerOf(WebView2CompositionControl webView, WebViewState state)
    {
        state.Controller ??= FindController(webView);
        return state.Controller;
    }

    private static CoreWebView2Controller? FindController(object root)
    {
        var queue = new Queue<object>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        queue.Enqueue(root);
        while (queue.Count > 0 && seen.Count < 24)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            for (var type = current.GetType(); type is not null; type = type.BaseType)
            {
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var field in type.GetFields(Flags))
                {
                    if (ReadMember(target => field.GetValue(target), current) is { } fieldValue && Consider(fieldValue, queue))
                    {
                        return (CoreWebView2Controller)fieldValue;
                    }
                }

                foreach (var property in type.GetProperties(Flags))
                {
                    if (property.GetIndexParameters().Length > 0 || !property.CanRead)
                    {
                        continue;
                    }

                    if (ReadMember(target => property.GetValue(target), current) is { } propertyValue && Consider(propertyValue, queue))
                    {
                        return (CoreWebView2Controller)propertyValue;
                    }
                }
            }
        }

        return null;
    }

    private static object? ReadMember(Func<object, object?> read, object target)
    {
        try
        {
            return read(target);
        }
        catch (Exception ex) when (ex is TargetException or TargetInvocationException or FieldAccessException
            or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static bool Consider(object value, Queue<object> queue)
    {
        if (value is CoreWebView2Controller)
        {
            return true;
        }

        if (value is not string && value.GetType().Assembly == typeof(WebView2CompositionControl).Assembly)
        {
            queue.Enqueue(value);
        }

        return false;
    }

    private static void StretchSurface(WebView2CompositionControl webView)
    {
        webView.ApplyTemplate();
        var partName = typeof(WebView2CompositionControl)
            .GetField("PartImageName", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null) as string;
        if (partName is not null && webView.Template?.FindName(partName, webView) is Image image)
        {
            image.Stretch = Stretch.Fill;
            image.HorizontalAlignment = HorizontalAlignment.Stretch;
            image.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    private static void HookWindow(WebView2CompositionControl webView)
    {
        var state = StateOf(webView);
        if (state.Window is not null || Window.GetWindow(webView) is not { } window)
        {
            return;
        }

        state.Window = window;
        state.WindowSizeChanged = (_, _) => SyncBounds(webView);
        state.WindowStateChanged = (_, _) =>
            webView.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => SyncBounds(webView)));
        window.SizeChanged += state.WindowSizeChanged;
        window.StateChanged += state.WindowStateChanged;
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var webView = (WebView2CompositionControl)sender;
        var state = StateOf(webView);
        if (state.Window is null)
        {
            return;
        }

        if (state.WindowSizeChanged is not null)
        {
            state.Window.SizeChanged -= state.WindowSizeChanged;
        }

        if (state.WindowStateChanged is not null)
        {
            state.Window.StateChanged -= state.WindowStateChanged;
        }

        state.Window = null;
    }
}
