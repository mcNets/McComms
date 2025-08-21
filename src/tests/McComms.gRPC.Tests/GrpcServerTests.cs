using NUnit.Framework;
using Grpc.Core;
using McComms.Core;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class GrpcServerTests {
    private readonly CancellationTokenSource _serverCts = new();

    private readonly ServerCredentials _serverCredentials = ServerCredentials.Insecure;

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        TestContext.WriteLine($"OneTimeSetup");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        _serverCts.Cancel();
        _serverCts.Dispose();

        TestContext.WriteLine("Test cleanup.");
    }

    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues() {
        var server = new GrpcServer(credentials: _serverCredentials);
        Assert.Multiple(() => {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(GrpcServer.DEFAULT_HOST));
            Assert.That(server.Address.Port, Is.EqualTo(GrpcServer.DEFAULT_PORT));
        });
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues() {
        var host = "127.0.0.1";
        var port = 9001;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        Assert.Multiple(() => {
            Assert.That(server, Is.Not.Null);
            Assert.That(server.Address.Host, Is.EqualTo(host));
            Assert.That(server.Address.Port, Is.EqualTo(port));
        });
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void Start_WhenCalled_DoesNotThrowException() {
        // Arrange
        var host = "127.0.0.1";
        var port = 9002;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        var mockCallback = new Func<Commsproto.mcCommandRequest, Commsproto.mcCommandResponse>(r => new Commsproto.mcCommandResponse());

        Assert.DoesNotThrow(() => {
            server.Start(mockCallback);
            server.Stop();
        });
    }

    [Test]
    [Order(4)]
    public void Stop_WhenCalled_DoesNotThrowException() {
        var host = "127.0.0.1";
        var port = 9003;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        Assert.DoesNotThrow(() => server.Stop());
    }

    [Test]
    [Order(5)]
    public void SendBroadcast_ThrowException_WhenServerIsNotRunning() {
        var host = "127.0.0.1";
        var port = 9004;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        Assert.Throws<InvalidOperationException>(() => server.SendBroadcast(new Commsproto.mcBroadcast()));
        server.Stop();
    }

    [Test]
    [Order(6)]
    public void SendBroadcastAsync_ThrowException_WhenServerIsNotRunning() {
        var host = "127.0.0.1";
        var port = 9004;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await server.SendBroadcastAsync(new Commsproto.mcBroadcast()));
        server.Stop();
    }

    [Test]
    [Order(7)]
    public void Start_WhenCalledMultipleTimes_DoesNotThrowException() {
        var host = "127.0.0.1";
        var port = 9007;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);
        var mockCallback = new Func<Commsproto.mcCommandRequest, Commsproto.mcCommandResponse>(r => new Commsproto.mcCommandResponse());

        Assert.DoesNotThrow(() => {
            server.Start(mockCallback);
            server.Start(mockCallback); // Starting again should not throw any exceptions
            server.Stop();
        });
    }

    [Test]
    [Order(8)]
    public void Stop_WhenCalledMultipleTimes_DoesNotThrowException() {
        var host = "127.0.0.1";
        var port = 9008;
        var commsHost = new CommsAddress(host, port);
        var server = new GrpcServer(commsHost, _serverCredentials);

        Assert.DoesNotThrow(() => {
            server.Stop();
            server.Stop(); // Stopping again should not throw any exceptions        });
        });
    }
}