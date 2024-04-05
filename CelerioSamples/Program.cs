using System.Text;
using Celerio;

string hello = "hello, привет";
var bytes = Encoding.UTF8.GetBytes(hello);

Stream stream = new MemoryStream();

stream.Write(bytes);

stream.Position = 0;

HttpContext.Request.TryParse(stream, out var http);