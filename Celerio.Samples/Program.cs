using Celerio;
using CelerioSamples;
using Newtonsoft.Json;

var pipeline = new Pipeline();

pipeline.Authentication = new Authentication<AuthSample.Credentials>("Your Unknown Secret Key");

pipeline.MapGet("/ping", ()=>DateTime.Now);

pipeline.ConfigureCors(new Cors().AddOrigin("localhost:5000").AllowCredentials(true));

Server server = new Server(pipeline);
server.StartListening(5000);