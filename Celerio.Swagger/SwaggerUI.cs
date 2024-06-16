namespace Celerio;

public class SwaggerUI : ModuleBase
{
    public override void Initialize(Pipeline pipeline)
    {
        pipeline.Modules.Add(new StaticFiles(new Dictionary<string, StaticFiles.StaticFile>()
        {
            {"/swagger", new ("swagger-ui/index.html", "text/html")},
        }));

        pipeline.Modules.Add(new StaticDirectory("/swagger/", "swagger-ui"));
    }
}