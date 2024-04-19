using System.Reflection;
using System.Text;

namespace Celerio;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class Response : Attribute
{
    public Type? Type { get; set; }
    public int StatusCode { get; set; }
    public string Description { get; set; }

    public Response(int statusCode, string description, Type? type = null)
    {
        Type = type;
        StatusCode = statusCode;
        Description = description;
    }
} 

public static class OpenApiGenerator
{
    public static string GenerateOpenApi(this EndpointRouter router, string title, string version = "1.0.0")
    {
        StringBuilder sb = new StringBuilder();

        //Header
        sb.AppendLine("openapi: 3.0.3");
        sb.AppendLine("info:");
        sb.AppendLine($"\ttitle: {title}");
        sb.AppendLine($"\tversion: {version}");
        
        
        //Services
        bool tags = false;
        HashSet<string> Services = new HashSet<string>();
        foreach (var ep in router.Endpoints)
        {
            if(ep.Service != null)
                if (!Services.Contains(ep.Service.Name))
                {
                    if (!tags)
                    {
                        tags = true;
                        sb.AppendLine("tags:");
                    }
                    Services.Add(ep.Service.Name);
                    sb.AppendLine($"\t- name: {ep.Service.Name}");
                    sb.AppendLine($"\t\tdescription: {ep.Service.Description}");
                }
        }
        
        
        //Endpoints
        sb.AppendLine("paths:");
        foreach (var ep in router.Endpoints)
        {
            foreach (var route in ep.Routes)
            {
                sb.AppendLine($"\t{route}:");
                sb.AppendLine($"\t\t{ep.HttpMethod.ToLower()}:");
                if (ep.Service != null)
                {
                    sb.AppendLine("\t\t\ttags: ");
                    sb.AppendLine($"\t\t\t\t- {ep.Service.Name}");
                }
                
                //Parameters
                var parameters = ep.Info.GetParameters();
                if(parameters.Length > 0)
                    sb.AppendLine("\t\t\tparameters:");
                foreach (var p in parameters)
                {
                    sb.AppendLine($"\t\t\t\t- name: {p.Name}");
                    sb.AppendLine($"\t\t\t\t\trequired: false");
                    sb.AppendLine($"\t\t\t\t\tin: query");
                }
                
                //Responses
                sb.AppendLine("\t\t\tresponses:");
                bool responses = false;
                foreach (var resp in ep.Info.GetCustomAttributes<Response>())
                {
                    responses = true;
                    sb.AppendLine($"\t\t\t\t\'{resp.StatusCode}\':");
                    sb.AppendLine($"\t\t\t\t\tdescription: {resp.Description}");
                }

                if (!responses)
                {
                    sb.AppendLine($"\t\t\t\t\'200\':");
                    sb.AppendLine($"\t\t\t\t\tdescription: OK");
                }
            }
        }
        
        return sb.ToString().Replace("\t", "  ");
    }
}