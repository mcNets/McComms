using NUnit.Framework;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class GrpcClientTests
{
    [Test]
    [Order(1)]
    public void Constructor_WithDefaultParameters_InitializesClientWithDefaultValues()
    {
        var client = new GrpcClient();
        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Host, Is.EqualTo(GrpcClient.DEFAULT_HOST));
            Assert.That(client.Port, Is.EqualTo(GrpcClient.DEFAULT_PORT));
        });
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        var host = "custom.host";
        var port = 9000;
        var client = new GrpcClient(host, port);
        Assert.That(client, Is.Not.Null);
        Assert.That(client.Host, Is.EqualTo(host));
        Assert.That(client.Port, Is.EqualTo(port));
    }

    [Test]
    [Order(3)]
    public void Dispose_WhenCalled_DoesNotThrowException()
    {
        var client = new GrpcClient();
        Assert.DoesNotThrow(() => client.Dispose());
    }

    [Test]
    [Order(4)]
    public async Task DisposeAsync_WhenCalled_DoesNotThrowException()
    {
        var client = new GrpcClient();
        await client.DisposeAsync();
        Assert.Pass();
    }

    [Test]
    [Order(5)]
    public void SendCommand_NotConnected_ReturnsNoSuccess()
    {
        var client = new GrpcClient();
        var response = client.SendCommand(new Commsproto.mcCommandRequest());
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
    }

    [Test]
    [Order(6)]
    public async Task SendCommandAsync_NotConnected_ReturnsNoSuccess()
    {
        var client = new GrpcClient();
        var response = await client.SendCommandAsync(new Commsproto.mcCommandRequest());
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
    }
}
