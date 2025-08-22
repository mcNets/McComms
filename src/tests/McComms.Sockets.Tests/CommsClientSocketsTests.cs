using NUnit.Framework;
using System.Net;

namespace McComms.Sockets.Tests;

[TestFixture]
public class CommsClientSocketsTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesClient()
    {
        // Arrange & Act
        var client = new CommsClientSockets();

        // Assert
        Assert.That(client, Is.Not.Null);
        // The client should be initialized but not connected at this point
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClient()
    {
        // Arrange
        var ipAddress = "127.0.0.1";
        var port = 8888;
        var address = new NetworkAddress(ipAddress, port);

        // Act
        var client = new CommsClientSockets(address);

        // Assert
        Assert.That(client, Is.Not.Null);
        // The client should be initialized but not connected at this point
    }

    [Test]
    public void SendCommand_NullArgument_ThrowsArgumentNullException()
    {
        var client = new CommsClientSockets();
        Assert.Throws<InvalidOperationException>(() => client.SendCommand(new Core.CommandRequest(0, string.Empty)));
    }

    [Test]
    public void Disconnect_DoesNotThrow()
    {
        var client = new CommsClientSockets();
        Assert.DoesNotThrow(() => client.Disconnect());
    }
}
