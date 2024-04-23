using Celerio;

var pipeline = new Pipeline();

pipeline.Modules.Add(new SwaggerGen());
pipeline.Modules.Add(new SwaggerUI());

Server server = new Server(pipeline);
await server.StartListening(5000);