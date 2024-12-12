namespace Celerio;

public class ModuleBase
{
    public virtual void Initialize(Pipeline pipeline){}

    public virtual HttpResponse? AfterRequest(Context context) { return null;}
    
    public virtual HttpResponse? BeforeEndpoint(Context context) { return null;}
    
    public virtual HttpResponse? AfterEndpoint(Context context, HttpResponse response) { return null;}
}