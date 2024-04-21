namespace Celerio;

public class SwaggerUI : ModuleBase
{
    public override void Initialize(Pipeline pipeline)
    {
        pipeline.Modules.Add(new StaticFiles(new Dictionary<string, StaticFiles.StaticFile>()
        {
            {"/swagger", new ("swagger-ui/index.html", "text/html")},
            
            {"/swagger/swagger-ui.css", new ("swagger-ui/swagger-ui.css", "text/css")},
            {"/swagger/swagger-ui.js", new ("swagger-ui/swagger-ui.js", "text/plain")},
            {"/swagger/swagger-ui-bundle.js", new ("swagger-ui/swagger-ui-bundle.js", "text/plain")},
            {"/swagger/swagger-ui-standalone-preset.js", new ("swagger-ui/swagger-ui-standalone-preset.js", "text/plain")}
        }));
    }
}