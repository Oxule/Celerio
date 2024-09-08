using Celerio;
using Newtonsoft.Json;

var pipeline = new Pipeline();

pipeline.Authentification = new DefaultAuthentification("Your Unknown Secret Key");

pipeline.Cors.AddOrigin("*");

pipeline.SetAuthDataType(typeof(long));

Server server = new Server(pipeline);
await server.StartListening(5000);