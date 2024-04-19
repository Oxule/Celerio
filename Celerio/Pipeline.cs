using System.Diagnostics;
using System.Text;

namespace Celerio;

public interface IAfterRequest
{
    public HttpResponse? AfterRequestHandler(HttpRequest request, Pipeline pipeline);
}
public interface IInitializable
{
    public void Initialize(Pipeline pipeline);
}
public interface IBeforeEndpoint
{
    public HttpResponse? BeforeEndpointHandler(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters,
        Dictionary<string, string> auth, Pipeline pipeline);
}
public interface IAfterEndpoint
{
    public HttpResponse? AfterEndpointHandler(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters,
        Dictionary<string, string> auth, Pipeline pipeline, HttpResponse response);
}

public class Pipeline
{
    public IHttpProvider HttpProvider = new Http11ProtocolProvider();
    
    public EndpointRouter EndpointRouter = new ();
    
    public MethodInvoke MethodInvoke = new ();
    
    public IAuthentification Authentification = new DefaultAuthentification("SampleKey", "SampleSalt");
    
    public List<object> Modules = new (){new AuthentificatedCheck(), new Caching()};
    
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
            HttpResponse resp;
            try
            {
                resp = PipelineExecution(request);
                HttpProvider.SendResponse(stream, resp);
                stream.Close();
            }
            catch (Exception e)
            {
                resp = new HttpResponse(500, "Internal Server Error", new Dictionary<string, string>(), e.Message);
                HttpProvider.SendResponse(stream, resp); 
                Logging.Err(e.Message + '\n' + e.StackTrace);
                stream.Close();
            }
        }
        catch (Exception e)
        {
            Logging.Err(e.Message + '\n' + e.StackTrace);
            stream.Close();
        }
    }

    public HttpResponse PipelineExecution(HttpRequest request)
    {
        foreach (var handler in Modules)
        {
            if (handler is IAfterRequest handlerAfterRequest)
            {
                var resp = handlerAfterRequest.AfterRequestHandler(request, this);
                if (resp != null)
                    return resp;
            }
        }
        
        var ep = EndpointRouter.GetEndpoint(request, out var parameters);

        if(ep == null)
            return new HttpResponse(404, "Not Found", new Dictionary<string, string>(), "Not Found");
        
        var identity = Authentification.Authentificate(request);

        foreach (var handler in Modules)
        {
            if (handler is IBeforeEndpoint handlerBeforeEndpoint)
            {
                var resp = handlerBeforeEndpoint.BeforeEndpointHandler(request, ep, parameters, identity, this);
                if (resp != null)
                    return resp;
            }
        }
        
        var response =  MethodInvoke.ParameterizedInvoke(ep.Info, request, parameters, new MethodInvoke.InvokeOverride(typeof(HttpRequest), request, ""), new MethodInvoke.InvokeOverride(typeof(Pipeline), this, ""), new MethodInvoke.InvokeOverride(typeof(Dictionary<string, string>), identity, "auth"));

        foreach (var handler in Modules)
        {
            if (handler is IAfterEndpoint handlerAfterEndpoint)
            {
                var resp = handlerAfterEndpoint.AfterEndpointHandler(request, ep, parameters, identity, this, response);
                if (resp != null)
                    response = resp;
            }
        }
        
        return response;
    }

    public Pipeline()
    {
    }

    public void Initialize()
    {
        foreach (var handler in Modules)
        {
            if (handler is IInitializable handlerInit)
            {
                handlerInit.Initialize(this);
            }
        }
    }
}