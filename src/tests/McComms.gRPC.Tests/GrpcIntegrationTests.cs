using Commsproto;
using NUnit.Framework;
using System.Collections.Concurrent;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class GrpcIntegrationTests
{
    // Base port number - each test will use BasePort + test order
    private const int BasePort = 9000;
    private int GetTestPort(int testOrder) => BasePort + testOrder;

    private string Host = "127.0.0.1";
    //private CancellationTokenSource _serverCts = null!;

    private GrpcServer _server = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _server = new GrpcServer(Host, BasePort);
        _server.Start(onCommandReceived: (request) =>
        {
            if (request.Id == 100)
            {                 
                // Simulate a broadcast message
                var broadcastMessage = new mcBroadcast { Id = 100, Content = "TEST_BROADCAST" };
                _server.SendBroadcast(broadcastMessage);
            }
            else if (request.Id == 200)
            {
                return new mcCommandResponse { Success = true, Message = GenerateRandomLongString() };
            }
            return new mcCommandResponse { Success = true, Message = request.Content };
        });
        await Task.Delay(2000); // Give server time to start
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _server.Stop();
    }

    [Test]
    [Order(1)]
    public void ClientServer_SingleClient_SendCommandReceivesResponse()
    {
        // Create and connect client
        var client = new GrpcClient(Host, BasePort);
        var connected = client.Connect(onBroadcastReceived: (msg) => { 
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new mcCommandRequest { Id = 1,  Content = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(2)]
    public async Task ClientServer_SingleClient_SendCommandAsyncReceivesResponse()
    {
        // Create and connect client
        var client = new GrpcClient(Host, BasePort);
        var connected = await client.ConnectAsync(onBroadcastReceived: (msg) => { 
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new mcCommandRequest { Id = 1,  Content = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(3)]
    public void ClientServer_MultipleClients_AllReceiveResponses()
    {
        // Create and connect multiple clients
        var client1 = new GrpcClient(Host, BasePort);
        var client2 = new GrpcClient(Host, BasePort);

        var connected1 = client1.Connect((_) => { });
        var connected2 = client2.Connect((_) => { });

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Act - Send commands from both clients
        var request1 = new mcCommandRequest { Id = 2, Content = "CLIENT1" };
        var request2 = new mcCommandRequest { Id = 3, Content = "CLIENT2" };

        var response1 = client1.SendCommand(request1);
        var response2 = client2.SendCommand(request2);

        Assert.Multiple(() =>
        {
            Assert.That(response1, Is.Not.Null);
            Assert.That(response1.Success, Is.True);
            Assert.That(response1.Message, Is.EqualTo("CLIENT1"));

            Assert.That(response2, Is.Not.Null);
            Assert.That(response2.Success, Is.True);
            Assert.That(response2.Message, Is.EqualTo("CLIENT2"));
        });

        client1.Disconnect();
        client2.Disconnect();
    }

    [Test]
    [Order(4)]
    public async Task ClientServer_MultipleClientsAsync_AllReceiveResponses()
    {
        // Create and connect multiple clients
        var client1 = new GrpcClient(Host, BasePort);
        var client2 = new GrpcClient(Host, BasePort);

        var connected1 = await client1.ConnectAsync((_) => { });
        var connected2 = await client2.ConnectAsync((_) => { });

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Act - Send commands from both clients
        var request1 = new mcCommandRequest { Id = 2, Content = "CLIENT1" };
        var request2 = new mcCommandRequest { Id = 3, Content = "CLIENT2" };

        var response1 = await client1.SendCommandAsync(request1);
        var response2 = await client2.SendCommandAsync(request2);

        Assert.Multiple(() =>
        {
            Assert.That(response1, Is.Not.Null);
            Assert.That(response1.Success, Is.True);
            Assert.That(response1.Message, Is.EqualTo("CLIENT1"));

            Assert.That(response2, Is.Not.Null);
            Assert.That(response2.Success, Is.True);
            Assert.That(response2.Message, Is.EqualTo("CLIENT2"));
        });

        await client1.DisconnectAsync();
        await client2.DisconnectAsync();
    }

    [Test]
    [Order(5)]
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast()
    {
        // Setup broadcast message collection for each client
        var client1ReceivedMessages = new ConcurrentBag<mcBroadcast>();
        var client2ReceivedMessages = new ConcurrentBag<mcBroadcast>();

        // Create and connect clients with broadcast handlers
        var client1 = new GrpcClient(Host, BasePort);
        var client2 = new GrpcClient(Host, BasePort);

        var connected1 = client1.Connect(msg => client1ReceivedMessages.Add(msg));
        var connected2 = client2.Connect(msg => client2ReceivedMessages.Add(msg));

        Assert.Multiple(() =>
        {
            // Assert clients connected successfully
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        // Initiate broadcast
        client1.SendCommand(new mcCommandRequest { Id = 100, Content = "BROADCAST_TEST" });

        // Wait a bit for the broadcast to be received
        Thread.Sleep(5000);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(client1ReceivedMessages, Is.Not.Empty);
            Assert.That(client2ReceivedMessages, Is.Not.Empty);
        });

        var client1Broadcast = client1ReceivedMessages.First();
        var client2Broadcast = client2ReceivedMessages.First();

        Assert.Multiple(() =>
        {
            Assert.That(client1Broadcast.Id, Is.EqualTo(100));
            Assert.That(client1Broadcast.Content, Is.EqualTo("TEST_BROADCAST"));
            Assert.That(client2Broadcast.Id, Is.EqualTo(100));
            Assert.That(client2Broadcast.Content, Is.EqualTo("TEST_BROADCAST"));
        });

        // Cleanup
        client1.Disconnect();
        client2.Disconnect();
    }

    [Test]
    [Order(5)]
    public void ClientServer_Clients_ReceiveLongResponse()
    {
        var client = new GrpcClient(Host, BasePort);

        var connected = client.Connect((_) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = new mcCommandRequest { Id = 200, Content = "" };

        var response = client.SendCommand(request);

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message.Length, Is.EqualTo(2048));
        });

        client.Disconnect();
    }

    private static string GenerateRandomLongString()
    {
        var random = new Random();
        var length = 2048;
        // Length of the string to generate
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}
