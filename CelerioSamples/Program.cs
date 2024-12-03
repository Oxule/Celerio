using Celerio;
using CelerioSamples;
using Newtonsoft.Json;

var pipeline = new Pipeline();

pipeline.Authentification = new Authentification<AuthSample.Credentials>("Your Unknown Secret Key");

pipeline.Cors.AddOrigin("*");

Server server = new Server(pipeline);
await server.StartListening(5000);