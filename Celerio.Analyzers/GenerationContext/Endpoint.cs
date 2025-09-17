using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.GenerationContext;

public class Endpoint
{
    public string Method;
    public string Route;
    public IMethodSymbol Symbol;
        
    public Endpoint(string method, string route, IMethodSymbol symbol)
    {
        Method = method;
        Route = route;
        Symbol = symbol;
    }
}