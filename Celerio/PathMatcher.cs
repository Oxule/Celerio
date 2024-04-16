namespace Celerio;

public class PathMatcher
{ 
    public bool Match(string path, string pattern, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();
            
        var patternParts = pattern.Split('/');
        var pathParts = path.Split('/');

        if (pathParts[0] != "" || patternParts[0] != "")
            return false;
            
        int patternL = patternParts.Length - (patternParts[^1] == "" ? 1 : 0);
        int pathL = pathParts.Length - (pathParts[^1] == "" ? 1 : 0);
            
        if (patternL != pathL)
            return false;

        for (int i = 0; i < patternL; i++)
        {
            if (pathParts[i] != patternParts[i])
            {
                if (patternParts[i][0] == '{' && patternParts[i][^1] == '}')
                {
                    string paramName = patternParts[i].Substring(1, patternParts[i].Length-2);
                    parameters.Add(paramName, pathParts[i]);
                }
                else 
                    return false;
            }
                    
        }
            
        return true;
    }
}