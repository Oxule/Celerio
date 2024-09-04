using Celerio;

var pipeline = new Pipeline();

pipeline.Authentification = new DefaultAuthentification("Your Unknown Secret Key");

pipeline.Cors.Credentials = true;
pipeline.Cors.AddOrigin("https://habr.com");

pipeline.SetAuthDataType(typeof(long));

Server server = new Server(pipeline);
await server.StartListening(5000);