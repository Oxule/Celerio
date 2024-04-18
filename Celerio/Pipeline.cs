﻿using System.Diagnostics;
using System.Text;

namespace Celerio;

public class Pipeline
{
    public IHttpProvider HttpProvider = new Http11ProtocolProvider();
    
    public EndpointRouter EndpointRouter = new ();
    
    public MethodInvoke MethodInvoke = new ();
    
    public Authentification Authentification = new DefaultAuthentification();
    
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
        var ep = EndpointRouter.GetEndpoint(request, out var parameters);

        if(ep == null)
            return new HttpResponse(404, "Not Found", new Dictionary<string, string>(), "Not Found");
        
        var identity = Authentification.Authentificate(request);
        
        return MethodInvoke.ParameterizedInvoke(ep.Info, request, parameters, new MethodInvoke.InvokeOverride(typeof(HttpRequest), request, ""), new MethodInvoke.InvokeOverride(typeof(Pipeline), this, ""), identity);
    }
}