using McComms.Core;
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
            Assert.That(client.Address.Host, Is.EqualTo(DefaultNetworkSettings.DEFAULT_HOST));
            Assert.That(client.Address.Port, Is.EqualTo(DefaultNetworkSettings.DEFAULT_PORT));
        });
    }

    [Test]
    [Order(2)]
    public void Constructor_WithCustomParameters_InitializesClientWithCustomValues()
    {
        var host = "custom.host";
        var port = 9000;
        var address = new NetworkAddress(host, port);
        var client = new GrpcClient(address);
        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Address.Host, Is.EqualTo(host));
            Assert.That(client.Address.Port, Is.EqualTo(port));
        });
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

    [Test]
    [Order(7)]
    public void Dispose_CalledTwice_DoesNotThrowException()
    {
        var client = new GrpcClient();
        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
            client.Dispose();
        });
    }

    [Test]
    [Order(8)]
    public async Task DisposeAsync_CalledTwice_DoesNotThrowException()
    {
        var client = new GrpcClient();
        await client.DisposeAsync();
        Assert.DoesNotThrowAsync(async () =>
        {
            await client.DisposeAsync();
        });
    }
}