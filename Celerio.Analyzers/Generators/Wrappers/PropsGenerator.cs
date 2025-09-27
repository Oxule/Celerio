using System.Diagnostics;
using System.Globalization;
using System.Text;
using Celerio.Analyzers.Generators.Validators;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators;

public static class PropsGenerator
{
    private static PropProvider GetProvider(IParameterSymbol symbol, List<string> pathVariables)
    {
        if (pathVariables.Any(x => x == symbol.Name))
        {
            return DefaultPropProvider.PathProvider;
        }
        
        foreach (var provider in PropProvider.Registry.OrderBy(x=>x.PredicateOrder))
            if (provider.Predicate(symbol))
            {
                return provider;
            }

        return DefaultPropProvider.QueryProvider;
    }
    
    private static List<PropProvider> GetValidators(IParameterSymbol symbol)
    {
        List<PropProvider> validators = new();
        
        foreach (var provider in PropValidators.Registry)
            if (provider.Predicate(symbol))
            {
                validators.Add(provider);
            }

        return validators.OrderBy(x=>x.Complexity).ToList();
    }
    
    public static void GenerateProps(StringBuilder sb, List<string> pathVariables, IMethodSymbol symbol)
    {
        var props = new List<string>();

        var providers = new List<(IParameterSymbol,PropProvider)>();
        
        foreach (var p in symbol.Parameters)
        {
            string paramVar = $"parameter_{p.Name}";
            props.Add(paramVar);

            var provider = GetProvider(p, pathVariables);
            providers.Add((p,provider));
        }

        providers = providers.OrderByDescending(x => x.Item2.Complexity).SelectMany(x =>
        {
            var validators = GetValidators(x.Item1).Select(y => (x.Item1, y)).ToList();
            var p = new List<(IParameterSymbol,PropProvider)>(1+validators.Count){x};
            p.AddRange(validators);
            p.Reverse();
            return p;
        }).ToList();

        void EmitInvoke(StringBuilder sb, int tabs) =>
            WrappersGenerator.GenerateCall(symbol, sb, string.Join(", ", props), tabs);

        if (providers.Count == 0)
        {
            EmitInvoke(sb, 3);
            return;
        }
        
        sb.AppendLine("\t\t\ttry {");

        var emitters = new PropProvider.InternalEmitDelegate[providers.Count];
        for (int i = 0; i < providers.Count; i++)
        {
            var j = i;
            var next = i == 0 ? EmitInvoke : emitters[i - 1];
            emitters[i] = (builder, tab) => providers[j].Item2.Emit(providers[j].Item1, builder, tab, next);
        }

        emitters[emitters.Length - 1](sb, 4);
        
        sb.AppendLine("\t\t\t} catch (Exception e) {");
        sb.AppendLine("\t\t\t\treturn new Result(400).Text(e.ToString());");
        sb.AppendLine("\t\t\t}");
    }
}