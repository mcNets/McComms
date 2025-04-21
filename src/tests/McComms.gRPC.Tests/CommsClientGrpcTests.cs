using NUnit.Framework;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class CommsClientGrpcTests
{
    [Test]
    public void Constructor_WithDefaultParameters_InitializesClient()
    {
        var client = new CommsClientGrpc();
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithCustomParameters_InitializesClient()
    {
        var host = "localhost";
        var port = 50051;
        var client = new CommsClientGrpc(host, port);
        Assert.That(client, Is.Not.Null);
    }
}
