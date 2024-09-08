namespace Celerio;

public class Context
{
    public readonly Pipeline Pipeline;
    public readonly HttpRequest Request;
    public Dictionary<string, object?> Details = new ();
    public EndpointManager.Endpoint? Endpoint = null;
    public dynamic? Identity = null;

    public Context(Pipeline pipeline, HttpRequest request)
    {
        Pipeline = pipeline;
        Request = request;
    }
}