using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Celerio;
using Moq;

namespace Celerio.Tests;

[TestFixture]
public class ServerTests
{
    [Test]
    public void StartListening_InvalidPort_ThrowsArgumentOutOfRangeException()
    {
        var server = new Server();
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.StartListening(-1));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => server.StartListening(65536));
    }

    [Test]
    public async Task StartListening_ValidPort_StartsSuccessfully()
    {
        var server = new Server();

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1000); // Stop the server after a short time
            await server.StartListening(2351, cts.Token);
        }

        Assert.Pass("Server started and stopped successfully.");
    }
    
    [Test]
    public async Task StartListening_CancellationTokenStopsServer()
    {
        var server = new Server();

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1000); // Stop the server after a short time
            await server.StartListening(5000, cts.Token);
        }

        Assert.Pass("Server stopped without issues.");
    }

    [Test]
    public async Task StartListening_ClientConnection_AcceptsClient()
    {
        var server = new Server();

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1000); // Stop the server after a short time

            var task = server.StartListening(5000, cts.Token);

            // Simulate a client connection
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, 5000);
            }

            await task;
        }

        Assert.Pass("Client connection handled.");
    }

    [Test]
    public async Task StartListening_ExceptionDuringListening_LogsError()
    {
        var server = new Server();
        
        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1000); // Stop the server after a short time

            try
            {
                await server.StartListening(-1, cts.Token);
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert.Pass("Handled invalid port error correctly.");
            }
        }
    }

    [Test]
    public void Server_DefaultConstructor_InitializesSuccessfully()
    {
        var server = new Server();
        Assert.IsNotNull(server);
    }

    [Test]
    public void Server_CustomPipeline_InitializesSuccessfully()
    {
        var pipeline = new Mock<Pipeline>();
        var server = new Server(pipeline.Object);

        Assert.IsNotNull(server);
    }
    
    [Test]
    public async Task StartListening_PortAlreadyInUse_ThrowsAddressAlreadyInUseException()
    {
        var server = new Server();

        server.StartListening(5000);
        
        var exceptionThrown = false;
        try
        {
            await server.StartListening(5000);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown);
    }
    
    [Test]
    public async Task StartListening_ClientConnection_ErrorHandling()
    {
        var server = new Server();

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(1000); // Stop the server after a short time

            var task = server.StartListening(5000, cts.Token);

            // Simulate an error (e.g. server shutting down)
            // Here we can simulate a client trying to connect after server is canceled
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, 5000);
                }
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is SocketException || ex is ObjectDisposedException);
            }

            await task;
        }

        Assert.Pass("Error handled correctly during client connection.");
    }

    [Test]
    public async Task StartListening_CancellationAfterConnection_StopsServerGracefully()
    {
        var server = new Server();
        var cts = new CancellationTokenSource();
    
        // Create client connection in parallel
        var clientTask = Task.Run(async () =>
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, 5000);
            }
        });

        // Cancel the server after a short delay
        cts.CancelAfter(1000);

        // Start listening and handle cancellation
        var serverTask = server.StartListening(5000, cts.Token);
    
        await Task.WhenAll(clientTask, serverTask);
    
        Assert.Pass("Server stopped gracefully after client connection.");
    }
}