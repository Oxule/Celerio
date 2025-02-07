using Celerio;

namespace CelerioSamples;

public static class AuthSample
{
    public class Credentials
    {
        public long Id;

        public Credentials(long id)
        {
            Id = id;
        }
    }
    
    [Route("GET", "/auth/{id}")]
    public static HttpResponse Auth(Context context, long id)
    {
        return context.Pipeline.Authentication.SendAuthentication(new Credentials(id));
    }
    
    [Route("GET", "/authLegacy")]
    public static object AuthCheckLeagcy(Context context)
    {
        return ((Credentials)context.Identity!).Id;
    }
    
    [Route("GET", "/auth")]
    public static object AuthCheck(Credentials auth)
    {
        return auth.Id;
    }
    
    [Route("GET", "/authOptional")]
    public static object AuthCheckOptional(Credentials? auth)
    {
        if(auth != null)
            return auth.Id;
        return -1;
    }
}