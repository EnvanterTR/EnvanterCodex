using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EnvanterTR.SecurityAcceptance;

internal sealed class SmtpCaptureServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentQueue<string> _messages = new();
    private readonly Task _acceptLoop;

    public SmtpCaptureServer()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = AcceptLoopAsync();
    }

    public int Port { get; }

    public IReadOnlyCollection<string> Messages => _messages.ToArray();

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
                _ = HandleClientAsync(client, _shutdown.Token);
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested) { }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
        using (var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true })
        {
            await writer.WriteLineAsync("220 localhost EnvanterTR acceptance SMTP");
            while (!cancellationToken.IsCancellationRequested)
            {
                var command = await reader.ReadLineAsync(cancellationToken);
                if (command is null) return;
                var verb = command.Split(' ', 2)[0].ToUpperInvariant();
                switch (verb)
                {
                    case "EHLO":
                    case "HELO":
                        await writer.WriteLineAsync("250-localhost");
                        await writer.WriteLineAsync("250 8BITMIME");
                        break;
                    case "MAIL":
                    case "RCPT":
                        await writer.WriteLineAsync("250 OK");
                        break;
                    case "DATA":
                        await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                        var message = new StringBuilder();
                        while (await reader.ReadLineAsync(cancellationToken) is { } line && line != ".")
                            message.AppendLine(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line);
                        _messages.Enqueue(message.ToString());
                        await writer.WriteLineAsync("250 Message accepted");
                        break;
                    case "QUIT":
                        await writer.WriteLineAsync("221 Bye");
                        return;
                    default:
                        await writer.WriteLineAsync("250 OK");
                        break;
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Stop();
        try { await _acceptLoop; }
        catch (SocketException) when (_shutdown.IsCancellationRequested) { }
        _shutdown.Dispose();
    }
}
