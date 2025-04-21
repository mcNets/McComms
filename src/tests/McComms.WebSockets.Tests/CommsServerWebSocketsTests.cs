using NUnit.Framework;
using System.Net;

namespace McComms.WebSockets.Tests;

[TestFixture]
public class CommsServerWebSocketsTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesServer()
    {
        // Arrange & Act
        var server = new CommsServerWebSockets();

        // Assert
        Assert.That(server, Is.Not.Null);
        // The server should be initialized but not started at this point
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesServer()
    {
        // Arrange
        var host = "localhost";
        var port = 8889;

        // Act
        var server = new CommsServerWebSockets(host, port);

        // Assert
        Assert.That(server, Is.Not.Null);
        // The server should be initialized but not started at this point
    }

    // Note: More comprehensive tests would involve starting the server and testing
    // the command handling functionality with real or mock clients
}
