using System.Reflection;

namespace Celerio;

public class Authenticated : Attribute { }

public class AuthenticatedCheck : ModuleBase
{
    private static bool IsNullableReferenceType(ParameterInfo parameter)
    {
        return parameter
            .CustomAttributes
            .Any(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute" 
                         && attr.ConstructorArguments.Count > 0 
                         && attr.ConstructorArguments[0].Value is byte flag 
                         && flag == 2);
    }
    
    public override HttpResponse? BeforeEndpoint(Context context)
    {
        if (context.Identity == null)
        {
            foreach (var p in context.Endpoint!.Method.GetParameters())
            {
                if (p.Name == "auth")
                {
                    if (!IsNullableReferenceType(p))
                    {
                        return HttpResponse.Unauthorized();
                    }
                    break;
                }
            }

            if (context.Endpoint!.Method.GetCustomAttribute<Authenticated>() != null)
            {
                return HttpResponse.Unauthorized();
            }
        }

        return null;
    }
}