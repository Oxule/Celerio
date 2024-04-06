using System.Text;

namespace Celerio;

public interface IPipeline
{
    public void ProcessRequest(Stream stream);
}

public class Pipeline : IPipeline
{
    public IHTTPProvider HttpProvider = new HTTP11ProtocolProvider();
    
    //1.Message Parsing
    //2.Authorization
    //3.Routing
    //4.Endpoint Execution
    //5.Message Compositing

    public async void ProcessRequest(Stream stream)
    {
        //PARSING
        if (!HttpProvider.GetRequest(stream, out var request))
        {
            Logging.Warn("Error While Parsing Protocol. Disconnecting...");
            stream.Close();
            return;
        }
        Logging.Log($"Request Parsed Successfully: {request.Method} {request.URI}");
        HttpProvider.SendResponse(stream, HttpResponse.Ok(""));
    }
}