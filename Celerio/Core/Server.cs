using System.Net;
using System.Net.Sockets;

namespace Celerio;

public class Server
{
    private readonly Pipeline _pipeline;

    public async Task StartListening(int port = 5000, CancellationToken cancellationToken = default)
    {
        if (port <= 0 || port >= 65536)
            throw new ArgumentOutOfRangeException($"Port must be between 0-65536 ({port})");
        
        _pipeline.Build();
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        Logging.Log($"Server started at http://localhost:{port}");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await socket.AcceptAsync(cancellationToken);
                Logging.Log($"Processing connection from {clientSocket.RemoteEndPoint}");
                new Thread(() => _pipeline.ProcessRequest(new NetworkStream(clientSocket))).Start();
            }
            catch (OperationCanceledException)
            {
                Logging.Log("Server stopped.");
                break;
            }
            catch (Exception ex)
            {
                Logging.Log($"Error: {ex.Message}");
            }
        }

        socket.Close();
    }

    public Server()
    {
        _pipeline = new Pipeline();
    }

    public Server(Pipeline pipeline)
    {
        _pipeline = pipeline;
    }
}