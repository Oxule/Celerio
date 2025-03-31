namespace Celerio;

public class EndpointsTree
{
    private readonly Node _root;
    public class Node
    {
        public string? Part;
        public Endpoint? Endpoint;
        public List<Node>? Children;
    }

    private EndpointsTree(Node root)
    {
        _root = root;
    }

    public static EndpointsTree BuildTree(IEnumerable<Endpoint> endpoints)
    {
        var root = new Node { Part = string.Empty, Children = new List<Node>() };

        foreach (var endpoint in endpoints)
        {
            var parts = endpoint.Route._parts;
            var dynamicFlags = endpoint.Route._dynamic;
            var current = root;

            for (int i = 0; i < parts.Length; i++)
            {
                string segment = dynamicFlags[i] ? "*" : parts[i];

                if (current.Children == null)
                    current.Children = new List<Node>();

                var child = current.Children.FirstOrDefault(n => n.Part == segment);
                if (child == null)
                {
                    child = new Node { Part = segment, Children = new List<Node>() };
                    current.Children.Add(child);
                }
                current = child;
            }

            current.Endpoint = endpoint;
        }

        return new EndpointsTree(root);
    }
    
    public bool TryMatch(string path, out Endpoint? endpoint, out string[] dynamicValues)
    {
        endpoint = null;
        dynamicValues = Array.Empty<string>();

        var segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var dynamicList = new List<string>();
        var current = _root;

        foreach (var seg in segments)
        {
            if (current.Children == null)
                return false;

            var fixedChild = current.Children.FirstOrDefault(n => n.Part != "*" && n.Part == seg);
            if (fixedChild != null)
            {
                current = fixedChild;
                continue;
            }

            var dynamicChild = current.Children.FirstOrDefault(n => n.Part == "*");
            if (dynamicChild != null)
            {
                dynamicList.Add(seg);
                current = dynamicChild;
                continue;
            }

            return false;
        }

        if (current.Endpoint != null)
        {
            endpoint = current.Endpoint;
            dynamicValues = dynamicList.ToArray();
            return true;
        }

        return false;
    }
}