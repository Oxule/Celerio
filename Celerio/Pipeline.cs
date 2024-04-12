using System.Text;

namespace Celerio;

public class Pipeline
{
    public IHTTPProvider HttpProvider = new HTTP11ProtocolProvider();
    
    public EndpointRouter EndpointRouter = new EndpointRouter();
    
    public void ProcessRequest(Stream stream)
    {
        try
        {
            if (!HttpProvider.GetRequest(stream, out var request))
            {
                Logging.Warn("Error While Parsing Protocol. Disconnecting...");
                stream.Write(Encoding.UTF8.GetBytes(HttpProvider.ErrorMessage));
                stream.Flush();
                stream.Close();
                return;
            }
            Logging.Log($"Request Parsed Successfully: {request.Method} {request.URI}");
            HttpProvider.SendResponse(stream, PipelineExecution(request));
        }
        catch (Exception e)
        {
            Logging.Err(e.Message + '\n' + e.StackTrace);
            stream.Close();
        }
    }

    public HttpResponse PipelineExecution(HttpRequest request)
    {
        var ep = EndpointRouter.GetRoute(request);

        if(ep == null)
            return new HttpResponse(404, "Not Found", new Dictionary<string, string>(), "Not Found");
        
        return ep.Method.Invoke(request);
    }
}