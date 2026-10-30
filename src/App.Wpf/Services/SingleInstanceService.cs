using System.IO;
using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MarkdownNotes.Services;

/// <summary>Carries the path (or an empty string meaning "just activate") sent by a second instance.</summary>
/// <param name="path">The received text.</param>
internal sealed class PathReceivedEventArgs(string path) : EventArgs
{
    /// <summary>Gets the received text; empty when the second instance only asked to be brought to front.</summary>
    public string Path { get; } = path;
}

/// <summary>Keeps a single running instance and receives the file a second launch wants to open.</summary>
internal interface ISingleInstanceService : IDisposable
{
    /// <summary>Raised on a background thread when a second instance forwards a path.</summary>
    event EventHandler<PathReceivedEventArgs>? PathReceived;

    /// <summary>Claims the single instance.</summary>
    /// <returns>True for the first instance, false when another one already runs.</returns>
    bool TryAcquire();

    /// <summary>Sends a path (or nothing) to the first instance.</summary>
    /// <param name="path">File to open, or null to only activate the first instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the first instance received it.</returns>
    Task<bool> ForwardAsync(string? path, CancellationToken cancellationToken);

    /// <summary>Starts receiving forwarded paths (first instance only).</summary>
    void StartListening();
}

/// <inheritdoc />
internal sealed class SingleInstanceService(ILogger<SingleInstanceService> logger, string? instanceKey = null) : ISingleInstanceService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _mutexName = instanceKey is null
        ? AppConfig.SingleInstanceMutexName
        : $@"Local\{AppConfig.AppName}.{instanceKey}";

    private readonly string _pipeName = $"{AppConfig.AppName}.{Environment.UserName}.{instanceKey ?? "main"}";
    private Mutex? _mutex;
    private CancellationTokenSource? _listenCts;

    /// <inheritdoc />
    public event EventHandler<PathReceivedEventArgs>? PathReceived;

    /// <inheritdoc />
    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: false, _mutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
        }

        return createdNew;
    }

    /// <inheritdoc />
    public async Task<bool> ForwardAsync(string? path, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(2000, cancellationToken).ConfigureAwait(false);

            await using var writer = new StreamWriter(client, Utf8NoBom) { AutoFlush = true };
            await writer.WriteLineAsync((path ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException)
        {
            logger.LogWarning(ex, "Impossible de joindre la premiere instance");
            return false;
        }
    }

    /// <inheritdoc />
    public void StartListening()
    {
        _listenCts = new CancellationTokenSource();
        var token = _listenCts.Token;
        _ = Task.Run(() => ListenAsync(token), token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listenCts?.Cancel();
        _listenCts?.Dispose();
        _listenCts = null;
        _mutex?.Dispose();
        _mutex = null;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server, Utf8NoBom);
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is { Length: <= AppConfig.PipeMaxPathLength })
                {
                    PathReceived?.Invoke(this, new PathReceivedEventArgs(line));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Echec de reception sur le tube d'instance unique");
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
