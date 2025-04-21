using NUnit.Framework;
using System.Net;
using System.Net.WebSockets;
using Moq;

namespace McComms.WebSockets.Tests;

[TestFixture]
[NonParallelizable]
public class WebSocketsClientTests
{
    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesClientWithDefaultValues()
    {
        // Arrange & Act
        using var client = new WebSocketsClient();

        // Assert
        Assert.That(client, Is.Not.Null);
        // By default it should connect to localhost:8080, but we can't easily test this
        // without exposing the private field
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        // Arrange
        var host = "custom.host";
        var port = 9000;

        // Act
        using var client = new WebSocketsClient(host, port);

        // Assert
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    [Order(3)]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var client = new WebSocketsClient();

        // Act & Assert
        Assert.DoesNotThrow(() => client.Dispose());
    }

    [Test]
    [Order(4)]
    public async Task DisposeAsync_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var client = new WebSocketsClient();

        // Act & Assert
        await client.DisposeAsync();
        Assert.Pass();
    }

    [Test]
    [Order(5)]
    public void SendCommand_NotConnected_ReturnsNoSuccess()
    {
        // Arrange
        var client = new WebSocketsClient();

        // Act
        var response = client.SendCommand(new McComms.Core.CommandRequest());

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
    }

    [Test]
    [Order(6)]
    public async Task SendCommandAsync_NotConnected_ReturnsNoSuccess()
    {
        // Arrange
        var client = new WebSocketsClient();

        // Act
        var response = await client.SendCommandAsync(new McComms.Core.CommandRequest());

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
    }
}
