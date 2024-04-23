using System.Collections;
using System.Reflection;
using System.Text;

namespace Celerio;

public class SwaggerGen : ModuleBase
{
    public override void Initialize(Pipeline pipeline)
    {
        List<OpenApi.Tag> tags = FindAllServices(pipeline);
        List<OpenApi.Route> routes = FindAllRoutes(pipeline);
        List<OpenApi.SchemaObject> schemas = new List<OpenApi.SchemaObject>();
        
        OpenApi api = new OpenApi("Title", "1.0.0", "Description", tags, routes, schemas);
        File.WriteAllText("openapi.yml", api.Serialize());
        pipeline.Modules.Add(new StaticFiles(new Dictionary<string, StaticFiles.StaticFile>()
        {
            {"/openapi.yml", new ("openapi.yml", "text/plain")},
        }));
    }

    public static List<OpenApi.Tag> FindAllServices(Pipeline pipeline)
    {
        List<OpenApi.Tag> tags = new List<OpenApi.Tag>();

        HashSet<string> services = new HashSet<string>();
        
        foreach (var ep in pipeline.EndpointRouter.Endpoints)
        {
            if(ep.Service!=null)
                if (!services.Contains(ep.Service.Name))
                {
                    services.Add(ep.Service.Name);
                    tags.Add(new OpenApi.Tag(ep.Service.Name, ep.Service.Description));
                }
        }
        
        return tags;
    }
    
    //TODO: did'nt work for now :(
    public static List<OpenApi.SchemaObject> FindAllSchemas(Pipeline pipeline)
    {
        List<Type> types = new List<Type>();
        
        foreach (var ep in pipeline.EndpointRouter.Endpoints)
        {
            foreach (var p in ep.Info.GetParameters())
            {
                if(types.Contains(p.ParameterType))
                    continue;
                if (p.ParameterType.IsClass)
                {
                    var parameter = new Parameter(p);
                    if(!parameter.External)
                        continue;
                    
                    types.Add(p.ParameterType);
                }
            }

            foreach (var r in ep.Info.GetCustomAttributes<Response>())
            {
            }
        }
        
        return null;
    }

    public static List<OpenApi.Route> FindAllRoutes(Pipeline pipeline)
    {
        var routes = new List<OpenApi.Route>();
        
        foreach (var ep in pipeline.EndpointRouter.Endpoints)
        {
            var r = routes.FirstOrDefault(r => r.Path == ep.Routes[0]);
            if (r == null)
            {
                r = new OpenApi.Route(ep.Routes[0], new());
                routes.Add(r);
            }

            var e = new OpenApi.Route.Endpoint(ep.HttpMethod.ToLower(), new(), ep.Service?.Name, null, new());

            foreach (var p in ep.Info.GetCustomAttributes<Response>())
            {
                e.Responses.Add(new (p.StatusCode, p.Description, null));
            }
            
            r.Endpoints.Add(e);
        }
        
        return routes;
    }
    
    public static OpenApi.Object? DescribeType(Type type)
    {
        if (IsArray(type, out var element))
        {
            var d = DescribeType(element);
            if (d == null)
                return null;
            return new OpenApi.ObjectArray(d);
        }

        if (type.IsClass)
        {
            List<OpenApi.ObjectClass.Property> props = new ();
            foreach (var f in type.GetFields())
            {
                if (f.IsStatic || !f.IsPublic)
                {
                    var d = DescribeType(f.FieldType);
                    if (d == null)
                        continue;
                    props.Add(new (f.Name, d));
                }
            }
            return new OpenApi.ObjectClass(props);
        }
        
        if(type == typeof(string))
            return new OpenApi.ObjectType("string", null, "example");
        
        //TODO: Expand List
        if(type == typeof(int)||type == typeof(long))
            return new OpenApi.ObjectType("integer", null, "123");
        
        return null;
    }
    
    //TODO: Some later make that Lists also arrays
    private static bool IsArray(Type type, out Type? elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            if (elementType == null)
                return false;
            return true;
        }

        elementType = null;
        return false;
    }
}