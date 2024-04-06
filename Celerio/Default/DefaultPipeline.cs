using System.Text;

namespace Celerio.DefaultPipeline;

public class DefaultPipeline : IPipeline
{
    //1.Message Parsing
    //2.Authorization
    //3.Routing
    //4.Endpoint Execution
    //5.Message Compositing

    public async void ProcessRequest(Stream stream)
    {
        //Sample Answer...
        stream.Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\nServer: Celerio\nConnection: close\nContent-Type: text/plain\nContent-Length: 13\n\nHello, World!"));
        await stream.FlushAsync();
    }
}