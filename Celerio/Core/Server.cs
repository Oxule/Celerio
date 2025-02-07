using System.Net;
using System.Net.Sockets;

namespace Celerio;

public class Server
{
    private readonly Pipeline _pipeline;

    public void StartListening(int port = 5000, int backlog = 1000) => StartListeningAsync().Wait();
    
    public async Task StartListeningAsync(int port = 5000, int backlog = 1000, CancellationToken cancellationToken = default)
    {
        if (port <= 0 || port >= 65536)
            throw new ArgumentOutOfRangeException($"Port must be between 0-65536 ({port})");
        
        _pipeline.Build();
        
        var ipEndpoint = new IPEndPoint(IPAddress.Any, port);
        using Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(ipEndpoint);
        listenSocket.Listen(backlog: backlog);
        
        Logging.Log($"Server started at http://localhost:{port}");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await listenSocket.AcceptAsync(cancellationToken);
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);
                        Logging.Log($"Processing connection from {clientSocket.RemoteEndPoint}");
                        Connection.HandleConnection(networkStream, _pipeline.HttpProvider, _pipeline.PipelineExecution);
                    }
                    catch (Exception ex)
                    {
                        Logging.Err(ex.ToString());
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logging.Err(ex.ToString());
            }
        }
        
        Logging.Log("Server stopped.");
    }

    public Server() : this(new Pipeline())
    {
    }

    public Server(Pipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }
}