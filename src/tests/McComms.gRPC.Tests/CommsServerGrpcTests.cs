using NUnit.Framework;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class CommsServerGrpcTests
{
    private readonly CancellationTokenSource _serverCts = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestContext.WriteLine($"Starting tests.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serverCts.Cancel();
        _serverCts.Dispose();

        TestContext.WriteLine("Test cleanup.");
    }

    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues()
    {
        var server = new CommsServerGrpc();
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.CommsHost.Host, Is.EqualTo(GrpcServer.DEFAULT_HOST));
            Assert.That(server.CommsHost.Port, Is.EqualTo(GrpcServer.DEFAULT_PORT));
        });
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        var host = "127.0.0.1";
        var port = 9001;
        var server = new CommsServerGrpc(host, port);
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.CommsHost.Host, Is.EqualTo(host));
            Assert.That(server.CommsHost.Port, Is.EqualTo(port));
        });
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void Start_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var mockCallback = new Func<McComms.Core.CommandRequest, McComms.Core.CommandResponse>(r => new McComms.Core.CommandResponse(true, "Mock Id", "Mock response"));

        var host = "127.0.0.1";
        var port = 9002;
        var server = new CommsServerGrpc(host, port);
        Assert.DoesNotThrow(() =>
        {
            server.Start(mockCallback, _serverCts.Token);
            server.Stop();
        });
    }

    [Test]
    [Order(4)]
    public void Stop_WhenCalled_DoesNotThrowException()
    {
        var host = "127.0.0.1";
        var port = 9003;
        var server = new CommsServerGrpc(host, port);
        Assert.DoesNotThrow(() => server.Stop());
    }
    
    [Test]
    [Order(5)]
    public void SendBroadcast_WithoutCallingStart_ThrowsException()
    {
        var host = "127.0.0.1";
        var port = 9004;
        var server = new CommsServerGrpc(host, port);
        Assert.Throws<InvalidOperationException>(() => server.SendBroadcast(new McComms.Core.BroadcastMessage(1, "Test message")));        
        server.Stop();
    }    

    [Test]
    [Order(6)]
    public void SendBroadcastAsync_WithoutCallingStart_ThrowsException()
    {
        var host = "127.0.0.1";
        var port = 9004;
        var server = new CommsServerGrpc(host, port);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await server.SendBroadcastAsync(new McComms.Core.BroadcastMessage(1, "Test message")));
        server.Stop();
    }

}
