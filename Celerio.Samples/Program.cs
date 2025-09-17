using System.Net;
using Celerio;
using Celerio.Generated;

var r = new Request("GET", "/work/", new Dictionary<string, string>(), new HeaderCollection(), []);
EndpointRouter.Route(r);

var server = new Server(IPAddress.Any, 5000);
server.Start();

await Task.Delay(Timeout.Infinite);