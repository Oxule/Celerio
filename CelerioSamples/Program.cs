using System.Text;
using Celerio;
using Celerio.DefaultPipeline;

Server server = new Server(new DefaultPipeline());
await server.StartListening(5000);