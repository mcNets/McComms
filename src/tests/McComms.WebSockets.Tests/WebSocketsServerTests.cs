using NUnit.Framework;
using System.Net;
using Moq;

namespace McComms.WebSockets.Tests;

[TestFixture]
[NonParallelizable]
public class WebSocketsServerTests
{
    private readonly CancellationTokenSource _serverCts = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestContext.WriteLine($"Starting WebSocketsServer tests.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serverCts.Cancel();
        _serverCts.Dispose();

        TestContext.WriteLine("WebSocketsServer tests cleanup.");
    }

    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues()
    {
        // Arrange & Act
        var server = new WebSocketsServer();

        // Assert
        Assert.That(server, Is.Not.Null);
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 9001;

        // Act
        var server = new WebSocketsServer(host, port);

        // Assert
        Assert.That(server, Is.Not.Null);
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void Start_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 9002;
        var server = new WebSocketsServer(host, port);
        var mockCallback = new Func<McComms.Core.CommandRequest, McComms.Core.CommandResponse>(r => new McComms.Core.CommandResponse());

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            server.Start(mockCallback);
            server.Stop();
        });
    }

    [Test]
    [Order(4)]
    public void Stop_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 9003;
        var server = new WebSocketsServer(host, port);

        // Act & Assert
        Assert.DoesNotThrow(() => server.Stop());
    }

    [Test]
    [Order(5)]
    public void SendBroadcast_ThrowException_WhenServerIsNotRunning()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 9004;
        var server = new WebSocketsServer(host, port);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => server.SendBroadcast(new McComms.Core.BroadcastMessage()));
        server.Stop();
    }
}
