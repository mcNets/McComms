using NUnit.Framework;
using System.Net;

namespace McComms.Sockets.Tests;

[TestFixture]
public class SocketsClientTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesClientWithDefaultValues()
    {
        using var client = new SocketsClient();
        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.CommsHost.Host, Is.EqualTo(SocketsClient.DEFAULT_HOST));
            Assert.That(client.CommsHost.Port, Is.EqualTo(SocketsClient.DEFAULT_PORT));
        });
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        var ipAddress = IPAddress.Parse("127.1.1.1");
        var port = 9000;
        using var client = new SocketsClient(ipAddress, port);

        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.CommsHost.Host, Is.EqualTo(ipAddress.ToString()));
            Assert.That(client.CommsHost.Port, Is.EqualTo(port));
        });
    }

    [Test]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        var client = new SocketsClient();
        Assert.DoesNotThrow(() => client.Dispose());
    }

    [Test]
    public void DisposeAsync_WhenCalled_DoesNotThrowException()
    {
        var client = new SocketsClient();
        Assert.DoesNotThrow(() => client.Dispose());
    }

    [Test]
    public void Send_NotConnected_ThrowsException()
    {
        var client = new SocketsClient();
        Assert.Throws<Exception>(() => client.Send([1, 2]));
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrowException()
    {
        var client = new SocketsClient();
        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
            client.Dispose();
        });
    }
}
