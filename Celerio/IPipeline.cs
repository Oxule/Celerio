using System.Text;

namespace Celerio;

public interface IPipeline
{
    public void ProcessRequest(Stream stream);
}

public class Pipeline : IPipeline
{
    public IHTTPProvider HttpProvider = new HTTP11ProtocolProvider();
    
    
    public delegate bool BeforeRouteMethod(HttpRequest request, out HttpResponse response);
    public List<BeforeRouteMethod> BeforeRoute = new List<BeforeRouteMethod>();
    
    //1.Message Parsing
    //2.RoutingBefore[]
    //2.Routing
    //3.Authorization
    //5.EndpointBefore[]
    //4.Endpoint Execution

    public void ProcessRequest(Stream stream)
    {
        try
        {
            //PARSING
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
        foreach (var method in BeforeRoute)
            if (!method.Invoke(request, out var response))
                return response;
        
        
        var r = HttpResponse.Ok("<h1>Hello, World!</h1><h2>I'm Oxule!!!</h2>");
        r.Headers["Content-Type"] = "text/html";
        return r;
    }
}