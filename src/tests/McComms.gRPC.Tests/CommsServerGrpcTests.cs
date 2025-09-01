using McComms.Core;
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
            Assert.That(server.Address.Host, Is.EqualTo(DefaultNetworkSettings.DEFAULT_SERVER_HOST));
            Assert.That(server.Address.Port, Is.EqualTo(DefaultNetworkSettings.DEFAULT_PORT));
        });
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        var address = new NetworkAddress("127.0.0.1", 9001);
        var server = new CommsServerGrpc(address);
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(address.Host));
            Assert.That(server.Address.Port, Is.EqualTo(address.Port));
        });
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void Start_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var mockCallback = new Func<McComms.Core.CommandRequest, McComms.Core.CommandResponse>(r => new McComms.Core.CommandResponse(true, "Mock Id", "Mock response"));

        var address = new NetworkAddress("127.0.0.1", 9002);
        var server = new CommsServerGrpc(address);
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
        var address = new NetworkAddress("127.0.0.1", 9003);
        var server = new CommsServerGrpc(address);
        Assert.DoesNotThrow(() => server.Stop());
    }
    
    [Test]
    [Order(5)]
    public void SendBroadcast_WithoutCallingStart_ThrowsException()
    {
        var address = new NetworkAddress("127.0.0.1", 9004);
        var server = new CommsServerGrpc(address);
        Assert.Throws<InvalidOperationException>(() => server.SendBroadcast(new McComms.Core.BroadcastMessage(1, "Test message")));
        server.Stop();
    }    

    [Test]
    [Order(6)]
    public void SendBroadcastAsync_WithoutCallingStart_ThrowsException()
    {
        var address = new NetworkAddress("127.0.0.1", 9004);
        var server = new CommsServerGrpc(address);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await server.SendBroadcastAsync(new McComms.Core.BroadcastMessage(1, "Test message")));
        server.Stop();
    }

}
