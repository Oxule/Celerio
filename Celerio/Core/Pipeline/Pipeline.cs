using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Celerio;

public class Pipeline
{
    public IHttpProvider HttpProvider = new DefaultHttpProvider();
    
    public IAuthentication Authentication = new Authentication<string>(DateTime.Now.ToString("O"));
    
    internal HttpResponse PipelineExecution(HttpRequest request, EndPoint remote)
    {
        Context context = new Context(this, request, remote);
        
        foreach (var module in _modules)
        {
            var resp = module.AfterRequest(context);
            if (resp != null)
                return resp;
        }
        
        context.Identity = Authentication.Authenticate(request);

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
    
    public Pipeline(){}

    #region Endpoints
    
        private readonly EndpointManager _endpointManager = new ();
    
        public void Map(string method, string route, Delegate action) => _endpointManager.Map(method, route, action);
        public void MapGet(string route, Delegate action) => Map("GET", route, action);
        public void MapPost(string route, Delegate action) => Map("POST", route, action);
        public void MapDelete(string route, Delegate action) => Map("DELETE", route, action);
        public void MapPut(string route, Delegate action) => Map("PUT", route, action);
    
    #endregion

    #region Modules

        private List<ModuleBase> _modules = new (){new AuthenticatedCheck(), new Caching()};
        public Pipeline AddModule(ModuleBase module, bool singleton = false)
        {
            if (singleton)
                for (int i = 0; i < _modules.Count; i++)
                    if (_modules[i].GetType() == module.GetType())
                    {
                        _modules.RemoveAt(i);
                        i--;
                    }
            
            module.Initialize(this);
            _modules.Add(module);
            return this;
        }

        #endregion
}