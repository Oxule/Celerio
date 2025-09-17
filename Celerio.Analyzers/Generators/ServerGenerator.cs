namespace Celerio.Analyzers.Generators.EndpointGenerator;

public static class ServerGenerator
{
    private const string Server = @"using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Celerio.Generated {
    public class Server : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly SemaphoreSlim _concurrency;
        private readonly CancellationTokenSource _cts = new();
        private bool _started = false;
        private readonly int _perRequestTimeoutMs;

        public Server(IPAddress address, int port, int maxConcurrent = 1000, int perRequestTimeoutMs = 30_000)
        {
            _listener = new TcpListener(address, port);
            _concurrency = new SemaphoreSlim(maxConcurrent);
            _perRequestTimeoutMs = perRequestTimeoutMs;
        }

        public void Start()
        {
            if (_started) throw new InvalidOperationException(""Server already started"");
            _started = true;
            _listener.Start();
            Console.WriteLine($""[server] listening on {_listener.LocalEndpoint}"");
            Task.Run(AcceptLoop);
        }

        public async Task StopAsync()
        {
            if (!_started) return;
            _cts.Cancel();
            _listener.Stop();
            await Task.Delay(100);
            _started = false;
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var tcp = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    await _concurrency.WaitAsync(_cts.Token).ConfigureAwait(false);
                    _ = Task.Run(() => HandleConnectionAsync(tcp).ContinueWith(t => _concurrency.Release()));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($""[accept] error: {ex}"");
                }
            }
        }

        private async Task HandleConnectionAsync(TcpClient client)
        {
            var remote = client.Client.RemoteEndPoint;
            using (client)
            {
                client.NoDelay = true;
                var ns = client.GetStream();

                client.ReceiveTimeout = 60_000;
                client.SendTimeout = 60_000;

                var keepAlive = true;
                try
                {
                    while (keepAlive && !_cts.IsCancellationRequested)
                    {
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                        linkedCts.CancelAfter(_perRequestTimeoutMs);
                        Request req = null;
                        try
                        {
                            req = await HttpRequestParser.ParseAsync(ns, linkedCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            //Console.WriteLine($""[conn:{remote}] request timed out or cancelled"");
                            await new Result(408).Text(""Request Timeout"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false);
                            break;
                        }
                        catch (FormatException fex)
                        {
                            //Console.WriteLine($""[conn:{remote}] bad request: {fex.Message}"");
                            await new Result(400).Text(""Bad Request"").WriteResultAsync(ns).ConfigureAwait(false);
                            break;
                        }
                        catch (IOException ioex)
                        {
                            //Console.WriteLine($""[conn:{remote}] io error while reading: {ioex.Message}"");
                            break;
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($""[conn:{remote}] unexpected error while parsing: {ex}"");
                            await new Result(500).Text(""Internal Server Error"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false);
                            break;
                        }

                        if (req == null)
                        {
                            break;
                        }

                        if (req.Headers.Get(""Connection"") is string connHeader && connHeader.Equals(""close"", StringComparison.OrdinalIgnoreCase))
                            keepAlive = false;

                        //Console.WriteLine($""{req.Method} {req.Path}"");

                        Result result;
                        try
                        {
                            result = await EndpointRouter.Route(req).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($""[conn:{remote}] handler threw: {ex}"");
                            await new Result(500).Text(""Internal Server Error"").SetHeader(""Connection"", ""close"").WriteResultAsync(ns).ConfigureAwait(false);
                            break;
                        }

                        result.SetHeader(""Connection"", keepAlive?""keep-alive"":""close"");

                        await result.WriteResultAsync(ns);

                        if (!keepAlive)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    try { ns.Close(); } catch { }
                    try { client.Close(); } catch { }
                    //Console.WriteLine($""[conn:{remote}] closed"");
                }
            }
        }
        public void Dispose()
        {
            _cts.Cancel();
            _listener.Server?.Dispose();
            _concurrency?.Dispose();
            _cts?.Dispose();
        }
    }
}";
    
    public static string GenerateServer()
    {
        return Server;
    }
}