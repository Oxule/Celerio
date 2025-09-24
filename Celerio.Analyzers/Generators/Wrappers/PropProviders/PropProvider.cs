using System.Text;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators;

    public class PropProvider
    {
        public Predicate<IParameterSymbol> Predicate;
        public int PredicateOrder;
        public int Complexity;
        public delegate void InternalEmitDelegate(StringBuilder sb, int tab);
        public delegate void EmitDelegate(IParameterSymbol symbol, StringBuilder sb, int tab, InternalEmitDelegate? next);
        public EmitDelegate Emit;

        public PropProvider(Predicate<IParameterSymbol> predicate, int predicateOrder, int complexity, EmitDelegate emit)
        {
            Predicate = predicate;
            PredicateOrder = predicateOrder;
            Complexity = complexity;
            Emit = emit;
        }
        
        public static List<PropProvider> Registry = new()
        {
            SystemPropProviders.RequestProvider
        };
    }