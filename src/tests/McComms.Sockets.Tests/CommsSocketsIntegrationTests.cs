namespace McComms.Sockets.Tests;

[TestFixture]
[NonParallelizable]
public class CommsSocketsIntegrationTests
{
    // Base port number - each test will use BasePort + test order
    private const int _basePort = 8500;
    private static int GetTestPort(int testOrder) => _basePort + testOrder;

    private readonly string _host = "127.0.0.1";
    private CancellationTokenSource _serverCts = null!;

    private CommsServerSockets _server = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _serverCts = new CancellationTokenSource();
        _server = new CommsServerSockets(IPAddress.Parse(_host), _basePort);
          // Start the server with a message handler
        _server.Start(commandReceived: (request) =>
        {
            if (request.Id == 100)
            {                 
                // Simulate a broadcast message
                var broadcastMessage = new BroadcastMessage { Id = 100, Message = "TEST_BROADCAST" };
                _server.SendBroadcast(broadcastMessage);
            }
            else if (request.Id == 200)
            {
                return new CommandResponse { Success = true, Id = "200", Message = GenerateRandomLongString() };
            }
            return new CommandResponse { Success = true, Id = request.Id.ToString(), Message = request.Message };
        }, _serverCts.Token);
            
        await Task.Delay(2000); // Give server time to start
    }    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serverCts.Cancel();
        _serverCts.Dispose();
        _server.Stop();
    }

    [Test]
    [Order(1)]
    public void ClientServer_SingleClient_SendCommandReceivesResponse()
    {
        // Create and connect client
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var connected = client.Connect(onBroadcastReceived: (msg) => { 
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new CommandRequest { Id = 1, Message = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Id, Is.EqualTo("1"));
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(2)]
    public async Task ClientServer_SingleClient_SendCommandAsyncReceivesResponse()
    {
        // Create and connect client
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var connected = await client.ConnectAsync(onBroadcastReceived: (msg) => { 
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new CommandRequest { Id = 1, Message = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request);

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
    [Order(3)]
    public void ClientServer_MultipleClients_AllReceiveResponses()
    {
        // Create and connect multiple clients
        var client1 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var client2 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        var connected1 = client1.Connect((_) => { });
        var connected2 = client2.Connect((_) => { });

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Act - Send commands from both clients
        var request1 = new CommandRequest { Id = 2, Message = "CLIENT1" };
        var request2 = new CommandRequest { Id = 3, Message = "CLIENT2" };

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
        var client1 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var client2 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        var connected1 = await client1.ConnectAsync((_) => { });
        var connected2 = await client2.ConnectAsync((_) => { });

        // Assert clients connected successfully
        Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
        Assert.That(connected2, Is.True, "Client 2 failed to connect to server");

        // Act - Send commands from both clients
        var request1 = new CommandRequest { Id = 2, Message = "CLIENT1" };
        var request2 = new CommandRequest { Id = 3, Message = "CLIENT2" };

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

        client1.Disconnect();
        client2.Disconnect();
    }    [Test]
    [Order(5)]
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast()
    {
        // Setup broadcast message collection for each client
        var client1ReceivedMessages = new ConcurrentBag<BroadcastMessage>();
        var client2ReceivedMessages = new ConcurrentBag<BroadcastMessage>();
        var client1Event = new ManualResetEvent(false);
        var client2Event = new ManualResetEvent(false);

        // Create and connect clients with broadcast handlers
        var client1 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var client2 = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        var connected1 = client1.Connect(msg => {
            client1ReceivedMessages.Add(msg);
            client1Event.Set();
        });
        
        var connected2 = client2.Connect(msg => {
            client2ReceivedMessages.Add(msg);
            client2Event.Set();
        });

        Assert.Multiple(() =>
        {
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        // Initiate broadcast
        client1.SendCommand(new CommandRequest { Id = 100, Message = "BROADCAST_TEST" });

        // Wait for the broadcasts to be received (with timeout)
        bool client1Received = client1Event.WaitOne(TimeSpan.FromSeconds(10));
        bool client2Received = client2Event.WaitOne(TimeSpan.FromSeconds(10));
        
        Assert.Multiple(() =>
        {
            Assert.That(client1Received, Is.True, "Client 1 did not receive broadcast within timeout");
            Assert.That(client2Received, Is.True, "Client 2 did not receive broadcast within timeout");
            Assert.That(client1ReceivedMessages, Is.Not.Empty, "Client 1 did not receive any broadcast messages");
            Assert.That(client2ReceivedMessages, Is.Not.Empty, "Client 2 did not receive any broadcast messages");
        });

        var client1Broadcast = client1ReceivedMessages.First();
        var client2Broadcast = client2ReceivedMessages.First();

        Assert.Multiple(() =>
        {
            Assert.That(client1Broadcast.Id, Is.EqualTo(100));
            Assert.That(client1Broadcast.Message, Is.EqualTo("TEST_BROADCAST"));
            Assert.That(client2Broadcast.Id, Is.EqualTo(100));
            Assert.That(client2Broadcast.Message, Is.EqualTo("TEST_BROADCAST"));
        });

        // Cleanup
        client1.Disconnect();
        client2.Disconnect();
    }

    [Test]
    [Order(6)]
    public void ClientServer_Clients_ReceiveLongResponse()
    {
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        var connected = client.Connect((_) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = new CommandRequest { Id = 200, Message = "" };

        var response = client.SendCommand(request);

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response?.Message?.Length, Is.GreaterThan(2000), "Response should contain a long message");
        });

        client.Disconnect();
    }

    [Test]
    [Order(7)]
    public async Task ClientServer_AsyncDisconnect_ClosesConnection()
    {
        // Create and connect client
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var connected = await client.ConnectAsync((_) => { });
        
        Assert.That(connected, Is.True, "Client failed to connect to server");
          // Send a command to verify the connection works
        var request = new CommandRequest { Id = 1, Message = "TEST_BEFORE_DISCONNECT" };
        var response = await client.SendCommandAsync(request);
        
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        
        // Disconnect asynchronously
        //await client.SendExitCommandAsync();
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
