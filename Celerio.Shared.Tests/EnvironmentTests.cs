using Xunit;
using Celerio;

public class EnvironmentTests
{
    [Fact]
    public void AuthSecret_DefaultsToHashOfDateTimeWhenEnvVarNotSet()
    {
        var secret = Celerio.Environment.AUTH_SECRET;
        Assert.Equal(32, secret.Length); // SHA256 length
        Assert.NotNull(secret);
    }

    [Fact]
    public void Port_DefaultsTo5000WhenEnvVarNotSet()
    {
        var port = Celerio.Environment.PORT;
        Assert.Equal(5000, port);
    }
}
