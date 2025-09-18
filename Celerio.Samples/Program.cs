using System.Net;
using Celerio;
using Celerio.Generated;

var server = new Server(IPAddress.Any, 5000);
server.Start();
await Task.Delay(Timeout.Infinite);