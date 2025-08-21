using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NUnit.Framework;
using System.Net;

namespace McComms.Sockets.Tests;

[TestFixture]
[NonParallelizable]
public class SocketsServerTests
{
    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues()
    {
        var server = new SocketsServer();
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(SocketsServer.DEFAULT_HOST));
            Assert.That(server.Address.Port, Is.EqualTo(SocketsServer.DEFAULT_PORT));
        });
        server.Dispose();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        var ipAddress = IPAddress.Parse("127.1.1.1");
        var port = 9001;
        var server = new SocketsServer(ipAddress, port);
        Assert.Multiple(() =>
        {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(ipAddress.ToString()));
            Assert.That(server.Address.Port, Is.EqualTo(port));
        });
        server.Dispose();
    }

    [Test]
    [Order(3)]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        var server = new SocketsServer();
        Assert.DoesNotThrow(() => server.Dispose());
    }

    [Test]
    [Order(4)]
    public void Listen_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var host = IPAddress.Parse("127.0.0.1");
        var port = 9002;
        var server = new SocketsServer(host, port);
        var cancellationToken = CancellationToken.None;

        Assert.DoesNotThrow(() =>
        {
            server.Listen((_) => [], cancellationToken);
            server.Dispose();
        });
    }
}
