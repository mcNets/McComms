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
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 8888;

        // Act
        var client = new CommsClientSockets(ipAddress, port);

        // Assert
        Assert.That(client, Is.Not.Null);
        // The client should be initialized but not connected at this point
    }

    // Note: These tests would need a mock socket server or integration tests with a real server
    // Here we're just testing the initialization of the client classes
}
