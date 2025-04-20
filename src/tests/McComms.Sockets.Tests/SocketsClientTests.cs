using NUnit.Framework;
using McComms.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace McComms.Sockets.Tests;

[TestFixture]
public class SocketsClientTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesClientWithDefaultValues()
    {
        // Arrange & Act
        using var client = new SocketsClient();

        // Assert
        Assert.That(client, Is.Not.Null);
        // By default it should connect to localhost:8888, but we can't easily test this
        // without exposing the private field
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        // Arrange
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 9000;

        // Act
        using var client = new SocketsClient(ipAddress, port);

        // Assert
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var client = new SocketsClient();

        // Act & Assert
        Assert.DoesNotThrow(() => client.Dispose());
    }

    // Note: Additional tests would require mocking network connections
    // or setting up integration tests with a real server
}
