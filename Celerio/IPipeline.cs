namespace Celerio;

public interface IPipeline
{
    public void ProcessRequest(Stream stream);
}