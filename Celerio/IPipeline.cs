using System.Text;

namespace Celerio;

public interface IPipeline
{
    public void ProcessRequest(Stream stream);
}

public class Pipeline : IPipeline
{
    public IHTTPProvider HttpProvider = new HTTP11ProtocolProvider();
    
    //1.Message Parsing
    //2.Routing
    //3.Authorization
    //4.Endpoint Execution
    //5.Message Compositing

    public async void ProcessRequest(Stream stream)
    {
        try
        {
            while (true)
            {
                //PARSING
                if (!HttpProvider.GetRequest(stream, out var request))
                {
                    Logging.Warn("Error While Parsing Protocol. Disconnecting...");
                    stream.Write(Encoding.UTF8.GetBytes(HttpProvider.ErrorMessage));
                    stream.Flush();
                    stream.Close();
                    return;
                }
                Logging.Log($"Request Parsed Successfully: {request.Method} {request.URI}");
                HttpProvider.SendResponse(stream, HttpResponse.Ok(""));
            }
        }
        catch (Exception e)
        {
            Logging.Err(e.Message + '\n' + e.StackTrace);
            stream.Close();
        }
    }
}