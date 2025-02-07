using System.Net.Sockets;

namespace Celerio;

public interface IHttpProvider
{
    public enum HttpParsingError : byte
    {
        None = 0,
        Version = 1,
        Syntax = 2,
        Other = 3,
        HeaderTooLarge = 4,
        IncompleteRequest = 5,
        InvalidChunkSize = 6,
        ContentLengthMismatch = 7
    }
    public bool ParseRequest(NetworkStream stream, out HttpRequest request, out HttpParsingError error);
    public void SendResponse(NetworkStream stream, HttpResponse response);
}