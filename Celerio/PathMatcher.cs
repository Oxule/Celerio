namespace Celerio;

public class PathMatcher
{ 
    public bool Match(string path, string pattern, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();
            
        var pattern_parts = pattern.Split('/');
        var path_parts = path.Split('/');

        if (path_parts[0] != "" || pattern_parts[0] != "")
            return false;
            
        int pattern_l = pattern_parts.Length - (pattern_parts[^1] == "" ? 1 : 0);
        int path_l = path_parts.Length - (path_parts[^1] == "" ? 1 : 0);
            
        if (pattern_l != path_l)
            return false;

        for (int i = 0; i < pattern_l; i++)
        {
            if (path_parts[i] != pattern_parts[i])
            {
                if (pattern_parts[i][0] == '{' && pattern_parts[i][^1] == '}')
                {
                    string param_name = pattern_parts[i].Substring(1, pattern_parts[i].Length-2);
                    parameters.Add(param_name, path_parts[i]);
                }
                else 
                    return false;
            }
                    
        }
            
        return true;
    }
}