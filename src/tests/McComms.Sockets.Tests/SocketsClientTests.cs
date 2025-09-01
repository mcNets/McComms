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
            Assert.That(client.Address.Host, Is.EqualTo(DefaultNetworkSettings.DEFAULT_CLIENT_HOST));
            Assert.That(client.Address.Port, Is.EqualTo(DefaultNetworkSettings.DEFAULT_PORT));
        });
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        var host = "127.1.1.1";
        var port = 9000;
        var address = new NetworkAddress(host, port);
        using var client = new SocketsClient(address);

        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Address.Host, Is.EqualTo(host));
            Assert.That(client.Address.Port, Is.EqualTo(port));
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
        Assert.Throws<InvalidOperationException>(() => client.Send([1, 2]));
    }

    [Test]
    public void SendAsync_NotConnected_ThrowsException()
    {
        var client = new SocketsClient();
        Assert.Throws<InvalidOperationException>(() => client.SendAsync([1, 2]).GetAwaiter().GetResult());
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

    [Test]
    public void Send_WithoutConnect_ThrowsException()
    {
        var client = new SocketsClient();
        Assert.Throws<InvalidOperationException>(() => client.Send([0x01, 0x02, 0x03]));
    }

    [Test]
    public void Constructor_InvalidIpAddress_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => {
            var address = new NetworkAddress("invalid_ip", 12345);
            var client = new SocketsClient(address);
        });
    }

    [Test]
    public void Constructor_InvalidPort_ThrowsArgumentOutOfRangeException()
    {
        var address = new NetworkAddress("127.0.0.1", -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            var client = new SocketsClient(address);
        });
    }

}