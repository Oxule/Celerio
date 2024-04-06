using System.Text;
using Celerio;

Server server = new Server(new Pipeline());
await server.StartListening(5000);