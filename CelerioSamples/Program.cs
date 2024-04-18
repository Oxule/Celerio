using System.Text;
using Celerio;

var pipeline = new Pipeline();
Server server = new Server(pipeline);
await server.StartListening(5000);