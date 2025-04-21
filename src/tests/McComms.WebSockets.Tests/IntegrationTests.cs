using NUnit.Framework;
using System.Collections.Concurrent;
using McComms.Core;

namespace McComms.WebSockets.Tests;

[TestFixture]
// Tests in this class can now run in parallel since each uses a different port
public class IntegrationTests
{
    // Base port number - each test will use BasePort + test order
    private const int BasePort = 9000;
    
    private string LoopbackAddress = "127.0.0.1";
    private CancellationTokenSource _serverCts = null!;
    
    // Define a test port for each test based on its order
    private int GetTestPort(int testOrder) => BasePort + testOrder;

    [SetUp]
    public void Setup()
    {
        // Create a new cancellation token source for each test
        _serverCts = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown()
    {
        // Cancel any running servers
        _serverCts.Cancel();
        _serverCts.Dispose();
    }

    [Test]
    [Order(1)]
    public void ClientServer_SingleClient_SendCommandReceivesResponse()
    {
        // Arrange
        int testPort = GetTestPort(1);
        var server = new CommsServerWebSockets(LoopbackAddress, testPort);
        
        // Start server with command handler that echoes the command back with "Echo: " prefix
        server.Start(request => 
        {
            return new CommandResponse 
            { 
                Success = true, 
                Message = $"Echo: {request.Message}" 
            };
        }, _serverCts.Token);

        // Create and connect client
        var client = new CommsClientWebSockets(LoopbackAddress, testPort);
        var connected = client.Connect(null);

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new CommandRequest { Id = 1, Message = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Echo: TEST_COMMAND"));

        // Cleanup
        client.Disconnect();
        server.Stop();
    }

    [Test]
    [Order(2)]
    public async Task ClientServer_SingleClient_AsyncSendCommandReceivesResponse()
    {
        // Arrange
        int testPort = GetTestPort(2);
        var server = new CommsServerWebSockets(LoopbackAddress, testPort);
        
        // Start server with command handler
        server.Start(request => 
        {
            return new CommandResponse 
            { 
                Success = true, 
                Message = $"Async Echo: {request.Message}" 
            };
        }, _serverCts.Token);

        // Create and connect client
        var client = new CommsClientWebSockets(LoopbackAddress, testPort);
        var connected = await client.ConnectAsync(null);

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command asynchronously
        var request = new CommandRequest { Id = 1, Message = "TEST_COMMAND_ASYNC" };
        var response = await client.SendCommandAsync(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Async Echo: TEST_COMMAND_ASYNC"));

        // Cleanup
        client.Disconnect();
        server.Stop();
    }

    [Test]
    [Order(3)]
    public void ClientServer_MultipleClients_AllReceiveResponses()
    {
        // Arrange
        int testPort = GetTestPort(3);
        var server = new CommsServerWebSockets(LoopbackAddress, testPort);
        
        // Start server with command handler
        server.Start(request => 
        {
            return new CommandResponse 
            { 
                Success = true, 
                Message = $"Response to client: {request.Message}" 
            };
        }, _serverCts.Token);

        // Create and connect multiple clients
        var client1 = new CommsClientWebSockets(LoopbackAddress, testPort);
        var client2 = new CommsClientWebSockets(LoopbackAddress, testPort);
        
        var connected1 = client1.Connect(null);
        var connected2 = client2.Connect(null);

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Act - Send commands from both clients
        var request1 = new CommandRequest { Id = 2, Message = "CLIENT1" };
        var request2 = new CommandRequest { Id = 3, Message = "CLIENT2" };
        
        var response1 = client1.SendCommand(request1);
        var response2 = client2.SendCommand(request2);

        // Assert
        Assert.That(response1, Is.Not.Null);
        Assert.That(response1.Success, Is.True);
        Assert.That(response1.Message, Is.EqualTo("Response to client: CLIENT1"));

        Assert.That(response2, Is.Not.Null);
        Assert.That(response2.Success, Is.True);
        Assert.That(response2.Message, Is.EqualTo("Response to client: CLIENT2"));

        // Cleanup
        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    [Test]
    [Order(4)]
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast()
    {
        // Arrange
        int testPort = GetTestPort(4);
        var server = new CommsServerWebSockets(LoopbackAddress, testPort);
        
        // Start server
        server.Start(request => new CommandResponse { Success = true }, _serverCts.Token);

        // Setup broadcast message collection for each client
        var client1ReceivedMessages = new ConcurrentBag<BroadcastMessage>();
        var client2ReceivedMessages = new ConcurrentBag<BroadcastMessage>();

        // Create and connect clients with broadcast handlers
        var client1 = new CommsClientWebSockets(LoopbackAddress, testPort);
        var client2 = new CommsClientWebSockets(LoopbackAddress, testPort);
        
        var connected1 = client1.Connect(msg => client1ReceivedMessages.Add(msg));
        var connected2 = client2.Connect(msg => client2ReceivedMessages.Add(msg));

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Create a test broadcast message
        var broadcastMessage = new BroadcastMessage 
        { 
            Id = 5, 
            Message = "This is a broadcast test message" 
        };

        // Act - Send the broadcast
        server.SendBroadcast(broadcastMessage);

        // Wait a bit for the broadcast to be received
        Thread.Sleep(500);

        // Assert
        Assert.That(client1ReceivedMessages.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(client2ReceivedMessages.Count, Is.GreaterThanOrEqualTo(1));

        var client1Broadcast = client1ReceivedMessages.First();
        var client2Broadcast = client2ReceivedMessages.First();

        Assert.That(client1Broadcast.Id, Is.EqualTo(broadcastMessage.Id));
        Assert.That(client1Broadcast.Message, Is.EqualTo(broadcastMessage.Message));
        Assert.That(client2Broadcast.Id, Is.EqualTo(broadcastMessage.Id));
        Assert.That(client2Broadcast.Message, Is.EqualTo(broadcastMessage.Message));

        // Cleanup
        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    [Test]
    [Order(5)]
    public async Task ServerClientScenario_ComplexInteraction_WorksAsExpected()
    {
        // Arrange
        int testPort = GetTestPort(5);
        var server = new CommsServerWebSockets(LoopbackAddress, testPort);
        
        // Command counter to verify multiple requests
        int commandCounter = 0;
        
        // Start server with a more complex command handler
        server.Start(request => 
        {
            commandCounter++;

            // Respond differently based on the command
            return request.Id switch
            {
                0 => new CommandResponse { Success = true, Message = "System status: ONLINE" },
                1 => new CommandResponse { Success = true, Message = $"Processed data: {request.Message}" },
                _ => new CommandResponse { Success = false, Message = "Unknown command" },
            };
        }, _serverCts.Token);

        // Setup broadcast message collection
        var receivedBroadcasts = new ConcurrentBag<BroadcastMessage>();

        // Create and connect clients
        var client1 = new CommsClientWebSockets(LoopbackAddress, testPort);
        var client2 = new CommsClientWebSockets(LoopbackAddress, testPort);
        
        var connected1 = await client1.ConnectAsync(msg => receivedBroadcasts.Add(msg));
        var connected2 = await client2.ConnectAsync(msg => receivedBroadcasts.Add(msg));

        // Act - Multiple interactions
        
        // 1. Client 1 sends status request
        var statusRequest = new CommandRequest { Id = 0 };
        var statusResponse = await client1.SendCommandAsync(statusRequest);
        
        // 2. Client 2 sends data processing request
        var dataRequest = new CommandRequest { Id = 1, Message = "SAMPLE123" };
        var dataResponse = await client2.SendCommandAsync(dataRequest);
        
        // 3. Server broadcasts an update
        var updateBroadcast = new BroadcastMessage { Id = 5, Message = "System updated to version 2.0" };
        server.SendBroadcast(updateBroadcast);
        
        // Give time for the broadcast to be received
        await Task.Delay(500);
        
        // 4. Client 1 sends an invalid command
        var invalidRequest = new CommandRequest { Id = 3 };
        var invalidResponse = await client1.SendCommandAsync(invalidRequest);

        // Assert
        
        // Verify command responses
        Assert.That(statusResponse.Success, Is.True);
        Assert.That(statusResponse.Message, Is.EqualTo("System status: ONLINE"));
        
        Assert.That(dataResponse.Success, Is.True);
        Assert.That(dataResponse.Message, Is.EqualTo("Processed data: SAMPLE123"));
        
        Assert.That(invalidResponse.Success, Is.False);
        Assert.That(invalidResponse.Message, Is.EqualTo("Unknown command"));
        
        // Verify broadcast was received by both clients (2 clients = 2 entries)
        Assert.That(receivedBroadcasts, Has.Count.EqualTo(2));
        foreach(var broadcast in receivedBroadcasts)
        {
            Assert.That(broadcast.Id, Is.EqualTo(5));
            Assert.That(broadcast.Message, Is.EqualTo("System updated to version 2.0"));
        }
        
        // Verify command counter
        Assert.That(commandCounter, Is.EqualTo(3), "Server should have processed 3 commands");

        // Cleanup
        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }
}
