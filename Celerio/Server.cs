using System.Net;
using System.Net.Sockets;

namespace Celerio;

public class Server
{
    private readonly Pipeline _pipeline;

    public async Task StartListening(int port)
    {
        _pipeline.Build();
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        Logging.Log($"Server started at http://localhost:{port}");
        while (true)
        {
            var a = await socket.AcceptAsync();
            Logging.Log($"Processing connection from {a.RemoteEndPoint}");
            new Thread(() => _pipeline.ProcessRequest(new NetworkStream(a))).Start();
        }
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