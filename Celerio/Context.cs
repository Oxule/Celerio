﻿using System.Dynamic;
using System.Net;

namespace Celerio;

public class Context
{
    public readonly Pipeline Pipeline;
    public readonly HttpRequest Request;
    public EndpointManager.Endpoint? Endpoint = null;
    public EndPoint? Remote = null;
    public string[]? PathParameters;
    public dynamic Details = new ExpandoObject();
    public dynamic? Identity = null;

    public Context(Pipeline pipeline, HttpRequest request, EndPoint? endPoint)
    {
        Remote = endPoint;
        Pipeline = pipeline;
        Request = request;
    }
}