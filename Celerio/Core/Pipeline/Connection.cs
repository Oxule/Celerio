using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Celerio;

internal static class Connection
{
    internal delegate HttpResponse PipelineExecution(HttpRequest request, EndPoint remote);
    
    internal static void HandleConnection(NetworkStream stream, IHttpProvider httpProvider, PipelineExecution pipelineExecution)
    {
        try
        {
            while (true)
            {
                if (!httpProvider.ParseRequest(stream, out var request, out IHttpProvider.HttpParsingError reason))
                {
                    //TODO: other exceptions
                    switch (reason)
                    {
                        case IHttpProvider.HttpParsingError.Version:
                            httpProvider.SendResponseAsync(stream,
                                new HttpResponse(101, "Switching Protocols").SetHeader("Upgrade", "HTTP/1.1")
                                    .SetHeader("Connection", "Upgrade"));
                            break;
                        default:
                            httpProvider.SendResponseAsync(stream, HttpResponse.BadRequest("Wrong request: " + reason));
                            break;
                    }
                    continue;
                }

                #region Keep-Alive
                
                bool keepAlive = false;
                if (request.Headers.TryGetSingle("Connection", out var connection))
                {
                    if (connection == "keep-alive")
                        keepAlive = true;
                    else if (connection == "close")
                        keepAlive = false;
                    else
                    {
                        ThrowBadRequest("Unknown connection header value");
                        break;
                    }
                }
                
                #endregion
                
                var resp = pipelineExecution(request, stream.Socket.RemoteEndPoint!);

                resp.SetHeader("Connection", connection);
                
                httpProvider.SendResponseAsync(stream, resp);

                if (!keepAlive)
                    break;
                
                void ThrowBadRequest(string content)
                {
                    httpProvider.SendResponseAsync(stream,
                        new HttpResponse(400, "Bad Request").SetBody(content));
                }
            }
        }
        catch (IOException _)
        {
        }
        catch (SocketException _)
        {
        }
        stream.Close();
    }
}