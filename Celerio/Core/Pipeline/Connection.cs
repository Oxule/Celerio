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
                    if (reason == IHttpProvider.HttpParsingError.Version)
                        httpProvider.SendResponse(stream,
                            new HttpResponse(101, "Switching Protocols").SetHeader("Upgrade", "HTTP/1.1")
                                .SetHeader("Connection", "Upgrade"));
                    else
                        httpProvider.SendResponse(stream, HttpResponse.BadRequest("Wrong request"));
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
                
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var resp = pipelineExecution(request, stream.Socket.RemoteEndPoint!);

                resp.SetHeader("Connection", connection);
                
                httpProvider.SendResponse(stream, resp);
                sw.Stop();
                Logging.Log(
                    $"{stream.Socket.RemoteEndPoint} asked {request.Method} {request.URI}\n -[{resp.StatusCode}] {resp.StatusMessage} in {sw.ElapsedMilliseconds}ms");

                if (!keepAlive)
                    break;
                
                void ThrowBadRequest(string content)
                {
                    httpProvider.SendResponse(stream,
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