using Celerio;

var pipeline = new Pipeline();

pipeline.Authentification.DataType = typeof((int x, float y, bool z, string str));

Server server = new Server(pipeline);
await server.StartListening(5000);