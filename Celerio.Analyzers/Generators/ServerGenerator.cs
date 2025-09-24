namespace Celerio.Analyzers.Generators;

public static class ServerGenerator
{
    private const string Server = @"using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Celerio.Generated
{
    public class Server : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly int _maxConcurrent;
        private volatile bool _started;
        private readonly CancellationTokenSource _cts = new();
        private int _currentConnections = 0;
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly int _readBufferSize;
        private readonly int _perRequestTimeoutMs;
        private readonly SemaphoreSlim _acceptSemaphore = new(1, 1);

        public Server(IPAddress address, int port, int maxConcurrent = 1000, int readBufferSize = 16 * 1024, int perRequestTimeoutMs = 30_000)
        {
            _listener = new TcpListener(address, port);
            _maxConcurrent = Math.Max(1, maxConcurrent);
            _readBufferSize = readBufferSize;
            _perRequestTimeoutMs = perRequestTimeoutMs;
        }

        public void Start()
        {
            if (_started) throw new InvalidOperationException(""Server already started"");
            _started = true;

            ThreadPool.GetMinThreads(out var w, out var i);
            ThreadPool.SetMinThreads(Math.Max(64, w), Math.Max(64, i));
            try { System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency; } catch { }

            _listener.Start(1024);
            Console.WriteLine($""[server] listening on {_listener.LocalEndpoint}"");
            _ = AcceptLoopAsync();
        }

        public async Task StopAsync()
        {
            if (!_started) return;
            _cts.Cancel();
            _listener.Stop();
            await Task.Delay(50).ConfigureAwait(false);
            _started = false;
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (Volatile.Read(ref _currentConnections) >= _maxConcurrent)
                    {
                        await Task.Delay(1, _cts.Token).ConfigureAwait(false);
                        continue;
                    }

                    var socket = await _listener.AcceptSocketAsync().ConfigureAwait(false);
                    if (Interlocked.Increment(ref _currentConnections) > _maxConcurrent)
                    {
                        Interlocked.Decrement(ref _currentConnections);
                        try { socket.Shutdown(SocketShutdown.Both); } catch { }
                        try { socket.Close(); } catch { }
                        continue;
                    }

                    try
                    {
                        socket.NoDelay = true;
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 0);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 0);
                    }
                    catch { /* non-fatal */ }

                    _ = Task.Run(() => ProcessConnectionAsync(socket));
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) when (_cts.IsCancellationRequested) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($""[accept] error: {ex}"");
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessConnectionAsync(Socket socket)
        {
            var remote = socket.RemoteEndPoint;
            var buffer = _arrayPool.Rent(_readBufferSize);
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                using var ns = new NetworkStream(socket, ownsSocket: true);

                ns.ReadTimeout = System.Threading.Timeout.Infinite;
                ns.WriteTimeout = System.Threading.Timeout.Infinite;

                var keepAlive = true;

                while (keepAlive && !_cts.IsCancellationRequested)
                {
                    using var perReqCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                    perReqCts.CancelAfter(_perRequestTimeoutMs);

                    Request req = null;
                    try
                    {
                        req = await HttpRequestParser.ParseAsync(ns, perReqCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            await new Result(408).Text(""Request Timeout"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false);
                        }
                        catch { }
                        break;
                    }
                    catch (FormatException)
                    {
                        try { await new Result(400).Text(""Bad Request"").WriteResultAsync(ns).ConfigureAwait(false); } catch { }
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        try { await new Result(500).Text(""Internal Server Error"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false); } catch { }
                        Console.WriteLine($""[conn:{remote}] unexpected error while parsing: {ex}"");
                        break;
                    }

                    if (req == null) break;

                    req.Remote = remote;

                    if (req.Headers.Get(""Connection"") is string connHeader && connHeader.Equals(""close"", StringComparison.OrdinalIgnoreCase))
                        keepAlive = false;

                    Result result;
                    try
                    {
                        result = await global::Celerio.Generated.EndpointRouter.Route(req).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($""[conn:{remote}] handler threw: {ex}"");
                        try { await new Result(500).Text(""Internal Server Error"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false); } catch { }
                        break;
                    }

                    result.SetHeader(""Connection"", keepAlive ? ""keep-alive"" : ""close"");

                    try
                    {
                        await result.WriteResultAsync(ns).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (!keepAlive) break;
                }
            }
            finally
            {
                try { _arrayPool.Return(buffer); } catch { }
                Interlocked.Decrement(ref _currentConnections);
                try { socket.Dispose(); } catch { }
                //Console.WriteLine($""[conn:{remote}] closed"");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Server?.Dispose(); } catch { }
            try { _acceptSemaphore?.Dispose(); } catch { }
            _cts?.Dispose();
        }
    }
}
";
    
    public static string GenerateServer()
    {
        return Server;
    }
}