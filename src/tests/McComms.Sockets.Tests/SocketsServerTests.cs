using NUnit.Framework;
using System.Net;

namespace McComms.Sockets.Tests;

[TestFixture]
public class SocketsServerTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues()
    {
        // Arrange & Act
        using var server = new SocketsServer();

        // Assert
        Assert.That(server, Is.Not.Null);
        // By default it should listen on any address and default port
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        // Arrange
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 9001;

        // Act
        using var server = new SocketsServer(ipAddress, port);

        // Assert
        Assert.That(server, Is.Not.Null);
    }

    [Test]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var server = new SocketsServer();

        // Act & Assert
        Assert.DoesNotThrow(() => server.Dispose());
    }

    // Note: More comprehensive tests would involve starting the server and testing
    // its communication with clients, which would require integration tests
}
