using System.Dynamic;

namespace Celerio;

public class Context
{
    public readonly Pipeline Pipeline;
    public readonly HttpRequest Request;
    public EndpointManager.Endpoint? Endpoint = null;
    public dynamic Details = new ExpandoObject();
    public dynamic? Identity = null;

    public Context(Pipeline pipeline, HttpRequest request)
    {
        Pipeline = pipeline;
        Request = request;
    }
}