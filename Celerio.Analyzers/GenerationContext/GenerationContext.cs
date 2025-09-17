using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.GenerationContext;

public class GenerationContext
{
    public List<Endpoint> Endpoints = new ();
    public HashSet<ITypeSymbol> Types = new ();
}