using System.Diagnostics;
using System.Text;
using Celerio.Analyzers.GenerationContext;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators.EndpointGenerator;

public static class RouterGenerator
{
    public static string GenerateRouter(List<Endpoint> endpoints)
    {
        var methodGrouped = endpoints.GroupBy(x => x.Method).OrderByDescending(x => x.Count());

        var sb = new StringBuilder();
        sb.AppendLine("namespace Celerio.Generated {\n\tpublic static class EndpointRouter {");
        sb.AppendLine("\t\tpublic static async Task<Result> Route(Celerio.Request request) {");
        sb.AppendLine("\t\t\tvar path = request.Path.Replace(\'\\\\\',\'/\').TrimEnd(\'/\');");
        sb.AppendLine("\t\t\tif(path == \"\") path = \"/\";");
        sb.AppendLine("\t\t\tvar method = request.Method;");
        foreach (var method in methodGrouped)
        {
            sb.AppendLine($"\t\t\tif(method == \"{method.Key.ToUpper()}\") {{");

            var roots = method.Select(x => ParsePath(x, x.Route)).ToArray();
            var root = MergeRoots(roots);
            RestoreParent(root);
            sb.AppendLine();
            root.Visualize(sb);
            sb.AppendLine();
            OptimiseNode(root);
            OrderChildren(root);
            sb.AppendLine();
            root.Visualize(sb);
            sb.AppendLine();

            GenerateNodeStatement(root, sb, 4);

            sb.AppendLine("\t\t\t}");
        }

        sb.AppendLine("\t\t\treturn Result.NotFound();");
        sb.AppendLine("\t\t}");
        sb.AppendLine("\t}\n}");
        return sb.ToString();
    }

    private static void GenerateNodeStatement(TrieNode node, StringBuilder sb, int tab)
    {
        if (node.Type == TrieNode.TrieNodeType.Root)
        {
            foreach (var c in node.Children)
            {
                GenerateNodeStatement(c, sb, tab);
            }

            return;
        }

        var t = Tab(tab);
        var p = TraceNodePointer(node);
        string invoke = "";
        if (node.Endpoint != null)
            invoke =
                $"return await EndpointWrappers.{node.Endpoint.Symbol.GetFullSymbolPath().Replace('.', '_')}_Wrapper({string.Join(", ", ["request", ..TracePathVariables(node).Select(x => "path_" + x)])});";

        if (node.Type == TrieNode.TrieNodeType.Static)
        {
            if (node.Children.Count == 0)
            {
                sb.AppendLine($"{t}if(path.Length == {p + " + " + node.Length} && path[{p}] == '{node.Char}') {{");
            }
            else
            {
                sb.AppendLine($"{t}if(path.Length >= {p + " + " + node.Length} && path[{p}] == '{node.Char}') {{");
            }

            if (node.Endpoint != null)
            {
                if (node.Children.Count != 0)
                {
                    sb.AppendLine($"{t}\tif(path.Length == {p + " + " + node.Length}) {{");
                    sb.AppendLine($"{t}\t\t{invoke}");
                    sb.AppendLine($"{t}\t}}");
                }
                else
                {
                    sb.AppendLine($"{t}\t{invoke}");
                }
            }

            foreach (var c in node.Children)
            {
                GenerateNodeStatement(c, sb, tab + 1);
            }

            sb.AppendLine($"{t}}}");
        }

        if (node.Type == TrieNode.TrieNodeType.Dynamic)
        {
            var stoppers = node.Children.Where(x => x.Type == TrieNode.TrieNodeType.Static)
                .Select(x => $"path[pointer_{node.ParameterName}] == '{x.Char}'").ToArray();
            if (stoppers.Length > 0)
            {
                sb.AppendLine($"{t}int pointer_{node.ParameterName} = {p};");
                sb.AppendLine(
                    $"{t}for (; pointer_{node.ParameterName} < path.Length; pointer_{node.ParameterName}++) {{");
                sb.AppendLine($"{t}\tif({string.Join(" || ", stoppers)}) {{");
                sb.AppendLine($"{t}\t\tbreak;");
                sb.AppendLine($"{t}\t}}");
                sb.AppendLine($"{t}}}");
            }
            else
            {
                sb.AppendLine($"{t}int pointer_{node.ParameterName} = path.Length - 1;");
            }

            sb.AppendLine(
                $"{t}string path_{node.ParameterName} = path.Substring({p}, pointer_{node.ParameterName} - ({p}));");
            if (node.Endpoint != null)
            {
                if (node.Children.Count != 0)
                {
                    sb.AppendLine($"{t}if(path.Length == {p + $" + path_{node.ParameterName}.Length"}) {{");
                    sb.AppendLine($"{t}\t{invoke}");
                    sb.AppendLine($"{t}}}");
                }
                else
                {
                    sb.AppendLine($"{t}{invoke}");
                }
            }

            foreach (var c in node.Children)
            {
                GenerateNodeStatement(c, sb, tab);
            }
        }
    }

    private static string[] TracePathVariables(TrieNode node)
    {
        if (node.Parent != null)
        {
            if (node.Type != TrieNode.TrieNodeType.Dynamic)
                return TracePathVariables(node.Parent);
            return [..TracePathVariables(node.Parent), node.ParameterName];
        }

        return [];
    }

    private static string Tab(int count) => new('\t', count);

    private static string TraceNodePointer(TrieNode node)
    {
        if (node.Parent != null)
        {
            switch (node.Parent.Type)
            {
                case TrieNode.TrieNodeType.Static:
                    return TraceNodePointer(node.Parent) + " + " + node.Parent.Length;
                case TrieNode.TrieNodeType.Dynamic:
                    return TraceNodePointer(node.Parent) + " + path_" + node.Parent.ParameterName + ".Length";
                case TrieNode.TrieNodeType.Root:
                    return "0";
            }
        }

        return "0";
    }

    private class TrieNode
    {
        public enum TrieNodeType
        {
            Root,
            Static,
            Dynamic
        }

        public TrieNodeType Type;
        public char Char;
        public int Length = 1;
        public List<TrieNode> Children = new List<TrieNode>();
        public TrieNode? Parent = null;
        public string? ParameterName;
        public Endpoint? Endpoint;

        public TrieNode(TrieNodeType type, char c, string? parameterName = null, Endpoint? endpoint = null)
        {
            Type = type;
            Char = c;
            ParameterName = parameterName;
            Endpoint = endpoint;
        }

        public static TrieNode Static(char ch, Endpoint? endpoint = null) =>
            new(TrieNodeType.Static, ch, null, endpoint);

        public static TrieNode Dynamic(string name, Endpoint? endpoint = null) =>
            new(TrieNodeType.Dynamic, '\0', name, endpoint);

        public static TrieNode Root() => new(TrieNodeType.Root, '\0');

        //TODO: Remove visualization
        public void Visualize(StringBuilder sb)
        {
            var node = this;
            if (node == null) throw new System.ArgumentNullException(nameof(node));
            if (sb == null) throw new System.ArgumentNullException(nameof(sb));

            sb.AppendLine("// " + NodeLabel(node));

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                bool isLast = (i == node.Children.Count - 1);
                VisualizeChild(child, sb, "", isLast);
            }
        }

        private static void VisualizeChild(TrieNode node, StringBuilder sb, string prefix, bool isLast)
        {
            var connector = isLast ? "└── " : "├── ";
            sb.AppendLine("// " + prefix + connector + NodeLabel(node));

            var childPrefix = prefix + (isLast ? "    " : "│   ");

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                bool last = (i == node.Children.Count - 1);
                VisualizeChild(child, sb, childPrefix, last);
            }
        }

        private static string NodeLabel(TrieNode node)
        {
            switch (node.Type)
            {
                case TrieNode.TrieNodeType.Root:
                    return "[Root]" + EndpointSuffix(node);
                case TrieNode.TrieNodeType.Static:
                    string ch = node.Char == '\0' ? "'\\0'" : $"'{node.Char}'";
                    return $"Static({ch} +{node.Length - 1})[{TraceNodePointer(node)}]" + EndpointSuffix(node);
                case TrieNode.TrieNodeType.Dynamic:
                    var name = node.ParameterName ?? "";
                    return $"Dynamic{{{name}}}[{TraceNodePointer(node)}]" + EndpointSuffix(node);
                default:
                    return node.Type.ToString() + EndpointSuffix(node);
            }
        }

        private static string EndpointSuffix(TrieNode node)
        {
            if (node.Endpoint == null) return string.Empty;
            try
            {
                return " => " + node.Endpoint.Symbol.Name;
            }
            catch
            {
                return " => <endpoint>";
            }
        }
    }


    private static TrieNode ParsePath(Endpoint ep, string path)
    {
        var root = TrieNode.Root();
        var current = root;
        if (string.IsNullOrEmpty(path))
        {
            root.Endpoint = ep;
            return root;
        }

        int i = 0;
        while (i < path.Length)
        {
            char c = path[i];
            if (c == '{')
            {
                int start = i + 1;
                int end = path.IndexOf('}', start);
                string name;
                if (end == -1)
                {
                    int nextSlash = path.IndexOf('/', start);
                    if (nextSlash == -1)
                    {
                        name = path.Substring(start).Trim();
                        i = path.Length;
                    }
                    else
                    {
                        name = path.Substring(start, nextSlash - start).Trim();
                        i = nextSlash;
                    }
                }
                else
                {
                    name = path.Substring(start, end - start).Trim();
                    i = end + 1;
                }

                if (name == null) name = string.Empty;

                var dyn = current.Children.FirstOrDefault(n =>
                    n.Type == TrieNode.TrieNodeType.Dynamic && n.ParameterName == name);
                if (dyn == null)
                {
                    dyn = TrieNode.Dynamic(name);
                    dyn.Parent = current;
                    current.Children.Add(dyn);
                }

                current = dyn;
                continue;
            }

            var child = current.Children.FirstOrDefault(n => n.Type == TrieNode.TrieNodeType.Static && n.Char == c);
            if (child == null)
            {
                child = TrieNode.Static(c);
                child.Parent = current;
                current.Children.Add(child);
            }

            current = child;
            i++;
        }

        current.Endpoint = ep;
        return root;
    }

    private static TrieNode MergeRoots(TrieNode[] roots)
    {
        if (roots == null) throw new ArgumentNullException(nameof(roots));
        var combined = TrieNode.Root();

        foreach (var root in roots)
        {
            if (root == null) continue;

            foreach (var child in root.Children)
            {
                var existing = combined.Children.FirstOrDefault(n => NodesMatch(n, child));
                if (existing == null)
                {
                    combined.Children.Add(CloneNode(child));
                }
                else
                {
                    MergeNodes(existing, child);
                }
            }
        }

        return combined;
    }

    private static void RestoreParent(TrieNode node, TrieNode? parent = null)
    {
        if (parent != null)
        {
            node.Parent = parent;
        }

        foreach (var c in node.Children)
        {
            RestoreParent(c, node);
        }
    }

    private static bool NodesMatch(TrieNode a, TrieNode b)
    {
        if (a.Type != b.Type) return false;
        return a.Type switch
        {
            TrieNode.TrieNodeType.Static => a.Char == b.Char,
            TrieNode.TrieNodeType.Dynamic => string.Equals(a.ParameterName, b.ParameterName, StringComparison.Ordinal),
            TrieNode.TrieNodeType.Root => true,
            _ => false,
        };
    }

    private static TrieNode CloneNode(TrieNode node)
    {
        var copy = new TrieNode(node.Type, node.Char, node.ParameterName, node.Endpoint);
        foreach (var ch in node.Children)
        {
            copy.Children.Add(CloneNode(ch));
        }

        return copy;
    }

    private static void MergeNodes(TrieNode target, TrieNode source)
    {
        if (target.Endpoint == null && source.Endpoint != null)
            target.Endpoint = source.Endpoint;

        foreach (var srcChild in source.Children)
        {
            var match = target.Children.FirstOrDefault(n => NodesMatch(n, srcChild));
            if (match == null)
            {
                target.Children.Add(CloneNode(srcChild));
            }
            else
            {
                MergeNodes(match, srcChild);
            }
        }
    }

    private static void OptimiseNode(TrieNode node)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            OptimiseNode(node.Children[i]);
        }

        var parent = node.Parent;
        if (node.Type != TrieNode.TrieNodeType.Static || parent == null) return;
        if (parent.Type != TrieNode.TrieNodeType.Static) return;
        if (parent.Children.Count != 1) return;

        if (node.Char == '/' && parent.Endpoint != null) return;

        if (node.Endpoint != null && parent.Endpoint != null) return;
        
        int delta = node.Length;

        parent.Children = node.Children;
        parent.Length += delta;

        if (node.Endpoint != null && parent.Endpoint == null)
        {
            parent.Endpoint = node.Endpoint;
        }

        foreach (var c in parent.Children)
        {
            c.Parent = parent;
        }
    }

    private static void OrderChildren(TrieNode node)
    {
        node.Children = node.Children.OrderBy(x => x.Type == TrieNode.TrieNodeType.Dynamic ? 1 : 0).ToList();
        foreach (var c in node.Children)
        {
            OrderChildren(c);
        }
    }
}