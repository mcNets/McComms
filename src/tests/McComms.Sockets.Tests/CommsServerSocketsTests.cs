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
            Assert.That(server.Address.Host, Is.EqualTo(SocketsServer.DEFAULT_HOST));
            Assert.That(server.Address.Port, Is.EqualTo(SocketsServer.DEFAULT_PORT));
        });
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServer()
    {
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 8889;
        var server = new CommsServerSockets(ipAddress, port);
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
        var ipAddress = IPAddress.Parse(host);
        var port = 9002;
        var server = new CommsServerSockets(ipAddress, port);
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
        var ipAddress = IPAddress.Parse(host);
        var port = 9003;
        var server = new CommsServerSockets(ipAddress, port);
        Assert.DoesNotThrow(() => server.Stop());
    }
}
