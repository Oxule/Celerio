using Celerio;

var pipeline = new Pipeline();

pipeline.Authentification = new DefaultAuthentification("Your Unknown Secret Key");

pipeline.Cors.AddOrigin("https://www.google.com").AddOrigin("http://localhost:3000");

pipeline.SetAuthDataType(typeof(long));

Server server = new Server(pipeline);
await server.StartListening(5000);