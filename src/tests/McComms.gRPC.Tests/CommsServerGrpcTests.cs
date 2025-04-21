using NUnit.Framework;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class CommsServerGrpcTests
{
    private readonly CancellationTokenSource _serverCts = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestContext.WriteLine($"Starting tests.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serverCts.Cancel();
        _serverCts.Dispose();

        TestContext.WriteLine("Test cleanup.");
    }

    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesServerWithDefaultValues()
    {
        var server = new CommsServerGrpc();
        Assert.That(server, Is.Not.Null);
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesServerWithCustomValues()
    {
        var host = "127.0.0.1";
        var port = 9001;
        var server = new CommsServerGrpc(host, port);
        Assert.That(server, Is.Not.Null);
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void Start_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var mockCallback = new Func<McComms.Core.CommandRequest, McComms.Core.CommandResponse>(r => new McComms.Core.CommandResponse(true, "Mock Id","Mock response"));

        var host = "127.0.0.1";
        var port = 9002;
        var server = new CommsServerGrpc(host, port);
        Assert.DoesNotThrow(() =>
        {
            server.Start(mockCallback, _serverCts.Token);
            server.Stop();
        });
    }

    [Test]
    [Order(4)]
    public void Stop_WhenCalled_DoesNotThrowException()
    {
        var host = "127.0.0.1";
        var port = 9003;
        var server = new CommsServerGrpc(host, port);
        Assert.DoesNotThrow(() => server.Stop());
    }
}
