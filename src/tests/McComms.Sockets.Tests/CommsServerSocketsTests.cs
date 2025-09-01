using NUnit.Framework;
using System.Net;

namespace McComms.Sockets.Tests;

[TestFixture]
[NonParallelizable]
public class CommsServerSocketsTests
{
    private readonly CancellationTokenSource _serverCts = new();

    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServer()
    {
        var server = new CommsServerSockets();
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
    public void Constructor_WithCustomParameters_InitializesServer()
    {
        var ipAddress = "127.0.0.1";
        var port = 8889;
        var networkAddress = new NetworkAddress(ipAddress, port);
        var server = new CommsServerSockets(networkAddress);
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(ipAddress.ToString()));
            Assert.That(server.Address.Port, Is.EqualTo(port));
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
        var address = new NetworkAddress(host, port);
        var server = new CommsServerSockets(address);
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
        var address = new NetworkAddress(host, port);
        var server = new CommsServerSockets(address);
        Assert.DoesNotThrow(() => server.Stop());
    }
}
