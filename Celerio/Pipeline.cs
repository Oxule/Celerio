using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Celerio;

public class ModuleBase
{
    public virtual void Initialize(Pipeline pipeline){}

    public virtual HttpResponse? AfterRequest(Context context) { return null;}
    
    public virtual HttpResponse? BeforeEndpoint(Context context) { return null;}
    
    public virtual HttpResponse? AfterEndpoint(Context context, HttpResponse response) { return null;}
}

public class Pipeline
{
    public IHttpProvider HttpProvider = new Http11ProtocolProvider();
    
    public IAuthentification Authentification = new DefaultAuthentification("SampleKey");

    public void SetAuthDataType(Type type)
    {
        Authentification.DataType = type;
    }
    
    private EndpointManager _endpointManager = new ();
    
    internal List<ModuleBase> Modules = new (){new AuthentificatedCheck(), new Caching(), new CorsBlocker()};

    public CORS Cors = new ();
    
    public Pipeline AddModule(ModuleBase module)
    {
        module.Initialize(this);
        Modules.Add(module);
        return this;
    }
    
    internal void ProcessRequest(NetworkStream stream)
    {
        try
        {
            while (true)
            {
                if (!HttpProvider.GetRequest(stream, out var request))
                {
                    Logging.Warn($"({stream.Socket.RemoteEndPoint})Error While Parsing Protocol. Disconnecting...");
                    stream.Close();
                    return;
                }
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                Logging.Log($"({stream.Socket.RemoteEndPoint})Request Parsed Successfully: {request.Method} {request.URI}");
                var resp = PipelineExecution(request);
                HttpProvider.SendResponse(stream, resp);
                Logging.Log($"({stream.Socket.RemoteEndPoint})Response Sent in {sw.ElapsedMilliseconds}ms ({sw.ElapsedTicks}t)!");
            }
        }
        catch (Exception e)
        {
            stream.Close();
        }
    }

    private HttpResponse PipelineExecution(HttpRequest request)
    {
        Context context = new Context(this, request);
        foreach (var module in Modules)
        {
            var resp = module.AfterRequest(context);
            if (resp != null)
                return resp;
        }
        
        context.Identity = Authentification.Authentificate(request);

        var ep = _endpointManager.GetEndpoint(context.Request, out var pathParameters);
        if (ep == null)
            return HttpResponse.NotFound();
        
        context.Endpoint = ep;
        
        foreach (var module in Modules)
        {
            var resp = module.BeforeEndpoint(context);
            if (resp != null)
                return resp;
        }
        
        var response = _endpointManager.CallEndpoint(context, pathParameters);

        foreach (var module in Modules)
        {
            var resp = module.AfterEndpoint(context, response);
            if (resp != null)
                return resp;
        }
        
        return response;
    }

    public Pipeline()
    {
    }
}