namespace McComms.Sockets.Tests;

[TestFixture]
[NonParallelizable]
public class SocketsIntegrationTests
{
    // Base port number - each test will use BasePort + test order
    private const int BasePort = 50051;
    private int GetTestPort(int testOrder) => BasePort + testOrder;

    private readonly string _host = "127.0.0.1";
    private CancellationTokenSource _serverCts = null!;

    private SocketsServer _server = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _serverCts = new CancellationTokenSource();
        _server = new SocketsServer(new NetworkAddress(_host, BasePort));
        
        // Start the server with a message handler
        _ = Task.Run(async () => await _server.ListenAsync(
                onMessageReceived: (request) =>
                {
                    // Convert the framed message to a string for easier handling
                    var requestStr = SocketsHelper.Decode(request);
                    
                    // Check if it's a broadcast trigger message
                    if (requestStr.Equals("100:BROADCAST_TEST"))
                    {                 
                        // Simulate a broadcast message
                        string broadcastMessage = "MSG_BROADCAST";
                        _ = _server.SendBroadcastAsync(SocketsHelper.Encode(broadcastMessage));
                    }
                    else if (requestStr.Equals("200:LONG_TEST"))
                    {
                        var responseStr = GenerateRandomLongString();
                        return SocketsHelper.Encode(responseStr);
                    }
                    
                    // Echo back the request for normal messages
                    return SocketsHelper.Encode(requestStr);
                }, 
                _serverCts.Token), 
                _serverCts.Token);
            
        await Task.Delay(2000); // Give server time to start
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serverCts.Cancel();
        _serverCts.Dispose();
        _server.Dispose();
    }

    [Test]
    [Order(1)]
    public void ClientServer_SingleClient_SendCommandReceivesResponse()
    {
        var client = new SocketsClient(new NetworkAddress(_host, BasePort));
        var connected = client.Connect(onMessageReceived: (msg) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = "1:TEST_COMMAND";
        var response = client.Send(SocketsHelper.Encode(request));
        var responseStr = SocketsHelper.Decode(response);

        Assert.Multiple(() =>
        {
            Assert.That(responseStr, Is.Not.Null);
            Assert.That(responseStr, Is.EqualTo("1:TEST_COMMAND"));
        });

        client.Dispose();
    }

    [Test]
    [Order(2)]
    public async Task ClientServer_SingleClient_SendCommandAsyncReceivesResponse()
    {
        var client = new SocketsClient(new NetworkAddress(_host, BasePort));
        var connected = await client.ConnectAsync(onMessageReceived: (msg) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = "1:TEST_COMAND_ASYNC";
        var response = await client.SendAsync(SocketsHelper.Encode(request));
        var responseStr = SocketsHelper.Decode(response);

        Assert.Multiple(() =>
        {
            Assert.That(responseStr, Is.Not.Null);
            Assert.That(responseStr, Is.EqualTo("1:TEST_COMAND_ASYNC"));
        });

        client.Dispose();
    }

    [Test]
    [Order(3)]
    public void ClientServer_MultipleClients_AllReceiveResponses()
    {
        var client1 = new SocketsClient(new NetworkAddress(_host, BasePort));
        var client2 = new SocketsClient(new NetworkAddress(_host, BasePort));

        var connected1 = client1.Connect((_) => { });
        var connected2 = client2.Connect((_) => { });

        Assert.Multiple(() =>
        {
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        // Act - Send commands from both clients
        var request1 = "1:TEST_CLIENT1";
        var request2 = "2:TEST_CLIENT2";

        var response1 = client1.Send(SocketsHelper.Encode(request1));
        var response2 = client2.Send(SocketsHelper.Encode(request2));

        var responseStr1 = SocketsHelper.Decode(response1);
        var responseStr2 = SocketsHelper.Decode(response2);

        Assert.Multiple(() =>
        {
            Assert.That(responseStr1, Is.Not.Null);
            Assert.That(responseStr1, Is.EqualTo("1:TEST_CLIENT1"));
            
            Assert.That(responseStr2, Is.Not.Null);
            Assert.That(responseStr2, Is.EqualTo("2:TEST_CLIENT2"));
        });

        client1.Dispose();
        client2.Dispose();
    }

    [Test]
    [Order(4)]
    public async Task ClientServer_MultipleClientsAsync_AllReceiveResponses()
    {
        var client1 = new SocketsClient(new NetworkAddress(_host, BasePort));
        var client2 = new SocketsClient(new NetworkAddress(_host, BasePort));

        var connected1 = await client1.ConnectAsync((_) => { });
        var connected2 = await client2.ConnectAsync((_) => { });

        Assert.Multiple(() =>
        {
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        var request1 = "1:TEST_CLIENT1_ASYNC";
        var request2 = "2:TEST_CLIENT2_ASYNC";

        var response1 = await client1.SendAsync(SocketsHelper.Encode(request1));
        var response2 = await client2.SendAsync(SocketsHelper.Encode(request2));

        var responseStr1 = SocketsHelper.Decode(response1);
        var responseStr2 = SocketsHelper.Decode(response2);

        Assert.Multiple(() =>
        {
            Assert.That(responseStr1, Is.Not.Null);
            Assert.That(responseStr1, Is.EqualTo("1:TEST_CLIENT1_ASYNC"));
            
            Assert.That(responseStr2, Is.Not.Null);
            Assert.That(responseStr2, Is.EqualTo("2:TEST_CLIENT2_ASYNC"));
        });

        await client1.DisconnectAsync();
        await client2.DisconnectAsync();
    }

    [Test]
    [Order(5)]
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast()
    {
        var client1ReceivedMessages = new ConcurrentBag<string>();
        var client2ReceivedMessages = new ConcurrentBag<string>();

        var client1 = new SocketsClient(new NetworkAddress(_host, BasePort));
        var client2 = new SocketsClient(new NetworkAddress(_host, BasePort));

        var connected1 = client1.Connect(msg => client1ReceivedMessages.Add(SocketsHelper.Decode(msg.ToArray())));
        var connected2 = client2.Connect(msg => client2ReceivedMessages.Add(SocketsHelper.Decode(msg.ToArray())));

        Assert.Multiple(() =>
        {
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        // Initiate broadcast (1 client enough)
        var request = "100:BROADCAST_TEST";
        client1.Send(SocketsHelper.Encode(request));

        // Start background broadcast listener for both clients
        //var client1Task = client1.WaitBroadcastMessageAsync();
        //var client2Task = client2.WaitBroadcastMessageAsync();

        // Wait a bit for the broadcast to be received
        Thread.Sleep(3000);

        Assert.Multiple(() =>
        {
            Assert.That(client1ReceivedMessages, Is.Not.Empty, "Client 1 did not receive any broadcast messages");
            Assert.That(client2ReceivedMessages, Is.Not.Empty, "Client 2 did not receive any broadcast messages");
        });

        var client1Broadcast = client1ReceivedMessages.First();
        var client2Broadcast = client2ReceivedMessages.First();

        Assert.Multiple(() =>
        {
            Assert.That(client1Broadcast, Is.EqualTo("MSG_BROADCAST"));
            Assert.That(client2Broadcast, Is.EqualTo("MSG_BROADCAST"));
        });

        // Cleanup
        client1.Dispose();
        client2.Dispose();
    }

    [Test]
    [Order(6)]
    public void ClientServer_Clients_ReceiveLongResponse()
    {
        var client = new SocketsClient(new NetworkAddress(_host, BasePort));

        var connected = client.Connect((_) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = "200:LONG_TEST";
        var response = client.Send(SocketsHelper.Encode(request));
        var responseStr = SocketsHelper.Decode(response);

        Assert.Multiple(() =>
        {
            Assert.That(responseStr, Is.Not.Null);
            Assert.That(responseStr,  Has.Length.GreaterThan(2000), "Response should contain a long message");
        });

        client.Dispose();
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
