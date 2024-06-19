using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Celerio;

public class ModuleBase
{
    public virtual void Initialize(Pipeline pipeline){}

    public virtual HttpResponse? AfterRequest(HttpRequest request, Pipeline pipeline) { return null;}
    
    public virtual HttpResponse? BeforeEndpoint(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters,
        object? auth, Pipeline pipeline) { return null;}
    
    public virtual HttpResponse? AfterEndpoint(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters,
        object? auth, Pipeline pipeline, HttpResponse response) { return null;}
}

public class Pipeline
{
    public IHttpProvider HttpProvider = new Http11ProtocolProvider();
    
    public EndpointRouter EndpointRouter = new ();
    
    public EndpointInvoke EndpointInvoke = new ();
    
    public IAuthentification Authentification = new DefaultAuthentification("SampleKey");
    
    public List<ModuleBase> Modules = new (){new AuthentificatedCheck(), new Caching()};
    
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
        foreach (var module in Modules)
        {
            var resp = module.AfterRequest(request, this);
            if (resp != null)
                return resp;
        }
        
        var ep = EndpointRouter.GetEndpoint(request, out var parameters);

        if(ep == null)
            return new HttpResponse(404, "Not Found", new Dictionary<string, string>(), "Not Found");
        
        var identity = Authentification.Authentificate(request);

        foreach (var module in Modules)
        {
            var resp = module.BeforeEndpoint(request, ep, parameters, identity, this);
            if (resp != null)
                return resp;
        }
        
        var response = EndpointInvoke.Invoke(
            ep.Info, request, parameters,
            new (typeof(HttpRequest), request),
            new (typeof(Pipeline), this),
            new (Authentification.DataType, identity, "auth"));

        foreach (var module in Modules)
        {
            var resp = module.AfterEndpoint(request, ep, parameters, identity, this, response);
            if (resp != null)
                return resp;
        }
        
        return response;
    }

    public Pipeline()
    {
        JsonConvert.DefaultSettings = (() =>
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter { NamingStrategy = new DefaultNamingStrategy()});
            return settings;
        });
    }

    public void Initialize()
    {
        for (int i = 0; i < Modules.Count; i++)
        {
            Modules[i].Initialize(this);
        }
    }
}