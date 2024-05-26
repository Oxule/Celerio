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
        var schemas = new List<OpenApi.SchemaObject>();
        
        
        return schemas;
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

            var e = new OpenApi.Route.Endpoint(ep.HttpMethod.ToLower(), new(), ep.Service?.Name);

            foreach (var p in ep.Info.GetCustomAttributes<Response>())
            {
                e.Responses.Add(new (p.StatusCode, p.Description, DescribeType(p.Type)));
            }
            
            if(e.Responses.Count == 0)
                e.Responses.Add(new (200, "OK"));
            
            List<OpenApi.Route.Endpoint.Parameter> parameters = new List<OpenApi.Route.Endpoint.Parameter>();
            
            foreach (var p in ep.Info.GetParameters())
            {
                if (p.Name?.ToLower() == "body")
                {
                    e.RequestBody =
                        new OpenApi.Route.Endpoint.BodyRequest(DescribeType(p.ParameterType), !p.HasDefaultValue, null);
                    continue;
                }
                if(Parameter.InternalNames.Contains(p.Name.ToLower())||Parameter.InternalTypes.Contains(p.ParameterType))
                    continue;
                parameters.Add(new OpenApi.Route.Endpoint.Parameter(p.Name,DescribeType(p.ParameterType), !p.HasDefaultValue, null, IsInRoute(p.Name, ep.Routes[0])?"path":"query"));
            }

            if (parameters.Count > 0)
                e.Parameters = parameters;
            
            r.Endpoints.Add(e);
        }
        
        return routes;
    }
    
    public static OpenApi.Object? DescribeType(Type? type)
    {
        if (type == null)
            return null;
        
        if(type == typeof(string))
            return new OpenApi.ObjectType("string", null, "example");
        
        if (type.Name == "Nullable`1")
        {
            return DescribeType(type.GenericTypeArguments[0]);
        }
        
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
                if (f.IsPublic&&!f.IsStatic)
                {
                    var d = DescribeType(f.FieldType);
                    if (d == null)
                        continue;
                    props.Add(new (f.Name, d));
                }
            }
            return new OpenApi.ObjectClass(props);
        }
        
        if (type.IsEnum)
        {
            List<string> enums = new ();
            foreach (var e in type.GetEnumNames())
            {
                enums.Add(e);
            }

            return new OpenApi.ObjectType("string", null, enums[0], enums);
        }
        
        //TODO: Expand List
        if(type == typeof(int)||type == typeof(long))
            return new OpenApi.ObjectType("integer", null, "123");
        
        if(type == typeof(float)||type == typeof(double))
            return new OpenApi.ObjectType("number", null, "12.34");
        
        if(type == typeof(bool))
            return new OpenApi.ObjectType("boolean", null, "true");
        
        if(type == typeof(DateTime))
            return new OpenApi.ObjectType("date", null, "2021-01-01T13:02:22.95467");
        
        return null;
    }
    
    //TODO: Some later make that Lists also arrays(every Enumerator)
    private static bool IsArray(Type type, out Type? elementType)
    {
        if (type.Name == "List`1")
        {
            elementType = type.GenericTypeArguments[0];
            return true;
        }
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

    private static bool IsInRoute(string name, string route)
    {
        var p = "{" + name + "}";
        return route.Contains(p);
    }
}