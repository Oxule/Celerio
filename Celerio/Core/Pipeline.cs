using System.Diagnostics;
using System.Net;
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
    
    public IAuthentification Authentification = new Authentification<string>("SampleKey");
    
    public CORS Cors = new ();
    
    private EndpointManager _endpointManager = new ();
    
    private List<ModuleBase> _modules = new (){new AuthentificatedCheck(), new Caching(), new CorsFilter()};
    
    public void Map(string method, string route, Delegate action) => _endpointManager.Map(method, route, action);
    public void MapGet(string route, Delegate action) => Map("GET", route, action);
    public void MapPost(string route, Delegate action) => Map("POST", route, action);
    public void MapDelete(string route, Delegate action) => Map("DELETE", route, action);
    public void MapPut(string route, Delegate action) => Map("PUT", route, action);
    
    public Pipeline AddModule(ModuleBase module)
    {
        module.Initialize(this);
        _modules.Add(module);
        return this;
    }
    
    internal void ProcessRequest(NetworkStream stream)
    {
        try
        {
            while (true)
            {
                if (!HttpProvider.GetRequest(stream, out var request, out string reason))
                {
                    if(reason == "proto_wrong")
                        HttpProvider.SendResponse(stream, new HttpResponse(101, "Switching Protocols").SetHeader("Upgrade", "HTTP/1.1").SetHeader("Connection", "Upgrade"));
                    else
                        HttpProvider.SendResponse(stream, HttpResponse.BadRequest("Wrong request"));
                    continue;
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var resp = PipelineExecution(request, stream.Socket.RemoteEndPoint);
                HttpProvider.SendResponse(stream, resp);
                Logging.Log($"{stream.Socket.RemoteEndPoint} asked {request.Method} {request.URI}\n -[{resp.StatusCode}] {resp.StatusMessage} in {sw.ElapsedMilliseconds}ms");
            }
        }
        catch (Exception e)
        {
            stream.Close();
            Logging.Log($"{stream.Socket.RemoteEndPoint} connection closed");
        }
    }

    private HttpResponse PipelineExecution(HttpRequest request, EndPoint? remote)
    {
        Context context = new Context(this, request, remote);
        
        foreach (var module in _modules)
        {
            var resp = module.AfterRequest(context);
            if (resp != null)
                return resp;
        }
        
        context.Identity = Authentification.Authentificate(request);

        var ep = _endpointManager.GetEndpoint(context.Request, out var pathParameters);
        if (ep == null)
            return HttpResponse.NotFound();

        context.PathParameters = pathParameters;
        context.Endpoint = ep;
        
        foreach (var module in _modules)
        {
            var resp = module.BeforeEndpoint(context);
            if (resp != null)
                return resp;
        }
        
        var response = _endpointManager.CallEndpoint(context);

        foreach (var module in _modules)
        {
            var resp = module.AfterEndpoint(context, response);
            if (resp != null)
                return resp;
        }
        
        return response;
    }

    internal void Build()
    {
        _endpointManager.MapStatic();
    }
    
    public Pipeline()
    {
    }
}