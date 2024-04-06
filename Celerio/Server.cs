using System.Net;
using System.Net.Sockets;

namespace Celerio;

public class Server
{
    private IPipeline pipeline { get; }

    public async Task StartListening(int port)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        Logging.Log($"Server Started At localhost:{port}");
        while (true)
        {
            try
            {
                var a = await socket.AcceptAsync();
                Logging.Log($"Processing Request From {a.RemoteEndPoint}");
                new Thread(() => pipeline.ProcessRequest(new NetworkStream(a))).Start();
            }
            catch (Exception e)
            {
                Logging.Err(e.Message);
            }
        }
    }

    public Server(IPipeline pipeline)
    {
        this.pipeline = pipeline;
    }
}