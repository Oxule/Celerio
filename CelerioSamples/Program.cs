using System.Text;
using Celerio;

var pipeline = new Pipeline();

File.WriteAllText("openapi.yml",pipeline.EndpointRouter.GenerateOpenApi("Sample Api"));
pipeline.Modules.Add(new StaticFiles(new Dictionary<string, StaticFiles.StaticFile>()
{
    {"/openapi.yml", new ("openapi.yml", "text/plain")},
}));

pipeline.Modules.Add(new SwaggerUI());

Server server = new Server(pipeline);
await server.StartListening(5000);