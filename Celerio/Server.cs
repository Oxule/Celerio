﻿using System.Net;
using System.Net.Sockets;

namespace Celerio;

public class Server
{
    private Pipeline Pipeline { get; } = new();

    public async Task StartListening(int port)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        Logging.Log($"Server Started At http://localhost:{port}");
        while (true)
        {
            var a = await socket.AcceptAsync();
            Logging.Log($"Processing Request From {a.RemoteEndPoint}");
            new Thread(() => Pipeline.ProcessRequest(new NetworkStream(a))).Start();
        }
    }

    public Server()
    {
    }

    public Server(Pipeline pipeline)
    {
        Pipeline = pipeline;
    }
}