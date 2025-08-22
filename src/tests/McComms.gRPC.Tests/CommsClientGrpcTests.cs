using NUnit.Framework;
using McComms.Core;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class CommsClientGrpcTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesClient()
    {
        var client = new CommsClientGrpc();
        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Address.Host, Is.EqualTo(GrpcClient.DEFAULT_HOST));
            Assert.That(client.Address.Port, Is.EqualTo(GrpcClient.DEFAULT_PORT));
        });
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClient()
    {
        var host = "localhost";
        var port = 50051;
        var address = new NetworkAddress(host, port);
        var client = new CommsClientGrpc(address);
        Assert.Multiple(() =>
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Address.Host, Is.EqualTo(host));
            Assert.That(client.Address.Port, Is.EqualTo(port));
        });
    }

    [Test]
    public void OnBroadcastReceived_WhenAssigned_SetsCallbackCorrectly()
    {
        var client = new CommsClientGrpc();
        Action<BroadcastMessage> callback = _ => { /* do nothing */ };

        // Act
        client.OnBroadcastReceived = callback;
        var assignedCallback = client.OnBroadcastReceived;

        // Assert
        Assert.That(assignedCallback, Is.EqualTo(callback));
    }
    
    [Test]
    public void Disconnect_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var client = new CommsClientGrpc();

        // Act & Assert - should delegate to GrpcClient.Disconnect
        Assert.DoesNotThrow(() => client.Disconnect());
    }

}
