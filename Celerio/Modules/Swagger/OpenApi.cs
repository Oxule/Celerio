using System.Text;

namespace Celerio;

public class OpenApi
{
    //PLEASE!!!!!!!!
    //DONT LOOK!!!!!!
    //THIS COULD BE HARMFUL!!!!!!!
    //BUT AT LEAST IT WORKS
    //I'LL REWORK IT LATER!!!!
    
    #region Header
    
    public string Title;
    public string Version;
    public string? Description;
    
    public string GenerateHeader()
    {
        string description = "";
        if(Description != null)
            description = $"\n\tdescription: {Description}";
        return $"openapi: 3.0.0\ninfo:\n\ttitle: {Title}\n\tversion: {Version}{description}\n";
    }
    
    #endregion

    #region Tags

    public class Tag
    {
        public string Name;
        public string? Description;

        public Tag(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }

    public List<Tag> Tags;
    
    public string GenerateTags()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("tags:");
        foreach (var tag in Tags)
        {
            sb.AppendLine($"\t- name: {tag.Name}");
            if(tag.Description!=null)
                sb.AppendLine($"\t\tdescription: {tag.Description}");
        }
        
        return sb.ToString();
    }
    
    #endregion

    #region Objects
    
    public abstract class Object
    {
        public abstract string GetBody(int tabs);
    }
    public class ObjectSchemaReference : Object
    {
        public string Schema;
        
        public override string GetBody(int tabs)
        {
            string t = Tab(tabs);
            
            return $"{t}$ref: '#/components/schemas/{Schema}'\n";
        }

        public ObjectSchemaReference(string schema)
        {
            Schema = schema;
        }
    }
    public class ObjectType : Object
    {
        public string Type;
        public string? Format;
        public string? Example;
        public List<string>? Enum;
        
        public override string GetBody(int tabs)
        {
            StringBuilder sb = new StringBuilder();
            
            string t = Tab(tabs);
            
            sb.AppendLine($"{t}type: {Type}");
            if(Format!=null)
                sb.AppendLine($"{t}format: {Format}");
            if(Example!=null)
                sb.AppendLine($"{t}example: {Example}");
            if (Enum != null)
            {
                sb.AppendLine($"{t}enum:");
                foreach (var value in Enum)
                {
                    sb.AppendLine($"{t}\t- {value}");
                }
            }

            return sb.ToString();
        }

        public ObjectType(string type, string? format = null, string? example = null, List<string>? @enum = null)
        {
            Type = type;
            Format = format;
            Example = example;
            Enum = @enum;
        }
    }
    public class ObjectArray : Object
    {
        public Object Items;
        
        public override string GetBody(int tabs)
        {
            StringBuilder sb = new StringBuilder();

            string t = Tab(tabs);
            
            sb.AppendLine($"{t}type: array");
            sb.AppendLine($"{t}items:");
            sb.Append($"{Items.GetBody(tabs+1)}");
            
            return sb.ToString();
        }

        public ObjectArray(Object items)
        {
            Items = items;
        }
    }
    public class ObjectClass : Object
    {
        public class Property
        {
            public string Name;
            public Object Object;

            public Property(string name, Object o)
            {
                Name = name;
                Object = o;
            }
        }
        
        public List<Property> Items;
        
        public override string GetBody(int tabs)
        {
            StringBuilder sb = new StringBuilder();

            string t = Tab(tabs);
            
            sb.AppendLine($"{t}type: object");
            sb.AppendLine($"{t}properties:");
            foreach (var item in Items)
            {
                sb.AppendLine($"{t}\t{item.Name}:");
                sb.Append($"{item.Object.GetBody(tabs+2)}");
            }
            
            return sb.ToString();
        }

        public ObjectClass(List<Property> items)
        {
            Items = items;
        }
    }

    private static string Tab(int tabCount)
    {
        var t = new char[tabCount];
        for (int i = 0; i < tabCount; i++)
            t[i] = '\t';
        return new string(t);
    }
    
    #endregion
    
    #region Routes
    
    public class Route
    {
        public string Path;

        public class Endpoint
        {
            public string Method;
            public string? Tag;
            public string? Description;
            
            public class BodyRequest
            {
                public Object Schema;
                public bool Required;
                public string? Description;

                public BodyRequest(Object schema, bool required = true, string? description = null)
                {
                    Schema = schema;
                    Required = required;
                    Description = description;
                }
            }
            
            public BodyRequest? RequestBody;
            
            public class Parameter
            {
                public string Name;
                public bool Required;
                public string? Description;
                public string? In;
                public Object Schema;

                public Parameter(string name, Object schema, bool required = true, string? description = null, string? @in = null)
                {
                    Required = required;
                    Name = name;
                    Description = description;
                    In = @in;
                    Schema = schema;
                }
            }
            
            public List<Parameter>? Parameters;
            
            public class Response
            {
                public int StatusCode;
                public string Description;
                public Object? Schema;

                public Response(int statusCode, string description, Object? schema = null)
                {
                    StatusCode = statusCode;
                    Description = description;
                    Schema = schema;
                }
            }
            
            public List<Response> Responses;

            public Endpoint(string method, List<Response> responses, string? tag = null, string? description = null, List<Parameter>? parameters = null, BodyRequest? requestBody = null)
            {
                Method = method;
                Tag = tag;
                Description = description;
                RequestBody = requestBody;
                Parameters = parameters;
                Responses = responses;
            }
        }
        
        public List<Endpoint> Endpoints;

        public Route(string path, List<Endpoint> endpoints)
        {
            Path = path;
            Endpoints = endpoints;
        }
    }
    
    public List<Route> Routes;

    public string GenerateRoutes()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("paths:");

        foreach (var route in Routes)
        {
            sb.AppendLine($"\t{route.Path}:");
            foreach (var ep in route.Endpoints)
            {
                sb.AppendLine($"\t\t{ep.Method}:");
                if (ep.Tag != null)
                {
                    sb.AppendLine($"\t\t\ttags:");
                    sb.AppendLine($"\t\t\t\t- {ep.Tag}");
                    if (ep.Description != null)
                    {
                        sb.AppendLine($"\t\t\tdescription: {ep.Description}");
                        sb.AppendLine($"\t\t\tsummary: {ep.Description}");
                    }

                    if (ep.RequestBody != null)
                    {
                        sb.AppendLine($"\t\t\trequestBody:");
                        if(ep.RequestBody?.Description != null)
                            sb.AppendLine($"\t\t\t\tdescription: {ep.RequestBody?.Description}");
                        sb.AppendLine($"\t\t\t\tcontent:");
                        sb.AppendLine($"\t\t\t\t\tapplication/json:");
                        sb.AppendLine($"\t\t\t\t\t\tschema:");
                        sb.Append($"{ep.RequestBody?.Schema.GetBody(7)}");
                        sb.AppendLine($"\t\t\t\trequired: {ep.RequestBody?.Required}");
                    }
                    
                    if (ep.Parameters != null&&ep.Parameters.Count != 0)
                    {
                        sb.AppendLine($"\t\t\tparameters:");
                        foreach (var p in ep.Parameters)
                        {
                            sb.AppendLine($"\t\t\t\t- name: {p.Name}");
                            if(p.In != null)
                                sb.AppendLine($"\t\t\t\t\tin: {p.In}");
                            if(p.Description != null)
                                sb.AppendLine($"\t\t\t\t\tdescription: {p.Description}");
                            sb.AppendLine($"\t\t\t\t\trequired: {p.Required.ToString().ToLower()}");
                            sb.AppendLine($"\t\t\t\t\tschema:");
                            sb.Append($"{p.Schema.GetBody(6)}");
                        }
                    }
                    
                    sb.AppendLine($"\t\t\tresponses:");
                    foreach (var r in ep.Responses)
                    {
                        sb.AppendLine($"\t\t\t\t'{r.StatusCode}':");
                        sb.AppendLine($"\t\t\t\t\tdescription: {r.Description}");
                        if (r.Schema != null)
                        {
                            sb.AppendLine($"\t\t\t\t\tcontent:");
                            sb.AppendLine($"\t\t\t\t\t\tdefault:");
                            sb.AppendLine($"\t\t\t\t\t\t\tschema:");
                            sb.Append($"{r.Schema.GetBody(8)}");
                        }
                    }
                }
            }
        }
        
        return sb.ToString();
    }
    
    #endregion

    #region Components

        #region Schemas

            public class SchemaObject
            {
                public string Name;
                public class Property
                {
                    public string Name;
                    public Object Object;
                    public bool Required;

                    public Property(string name, Object o, bool required = true)
                    {
                        Name = name;
                        Object = o;
                        Required = required;
                    }
                }
                public List<Property> Properties;

                public SchemaObject(string name, List<Property> properties)
                {
                    Name = name;
                    Properties = properties;
                }
            }
            
            public List<SchemaObject> SchemasObjects;

            public string GenerateSchemas()
            {
                var sb = new StringBuilder();

                sb.AppendLine("components:");
                sb.AppendLine("\tschemas:");
                foreach (var o in SchemasObjects)
                {
                    sb.AppendLine($"\t\t{o.Name}:");
                    sb.AppendLine($"\t\t\ttype: object");
                    sb.AppendLine($"\t\t\tproperties:");
                    foreach (var p in o.Properties)
                    {
                        sb.AppendLine($"\t\t\t\t{p.Name}:");
                        sb.Append($"{p.Object.GetBody(5)}");
                    }
                }
                
                return sb.ToString();
            }

        #endregion

        #region Authorizations

            //TODO: Implement

        #endregion

    #endregion

    public OpenApi(string title, string version, string? description, List<Tag> tags, List<Route> routes, List<SchemaObject> schemas)
    {
        Title = title;
        Version = version;
        Description = description;
        Tags = tags;
        Routes = routes;
        SchemasObjects = schemas;
    }

    public string Serialize()
    {
        return (GenerateHeader() + GenerateTags() + GenerateRoutes() + GenerateSchemas()).Replace("\t", "  ");
    }
}