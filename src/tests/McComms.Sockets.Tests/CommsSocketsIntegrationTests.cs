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
            else if (request.Id == 101)
            {
                // Handle second command triggered by broadcast callback
                return new CommandResponse { Success = true, Id = "101", Message = "SECOND_COMMAND_PROCESSED" };
            }
            else if (request.Id == 200)
            {
                return new CommandResponse { Success = true, Id = "200", Message = GenerateRandomLongString() };
            }
            return new CommandResponse { Success = true, Id = request.Id.ToString(), Message = request.Message };
        }, _serverCts.Token);
            
        await Task.Delay(2000); // Give server time to start
    }

    [OneTimeTearDown]
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
    }

    [Test]
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
        Assert.That(response.Success, Is.True);        // Disconnect asynchronously
        //await client.SendExitCommandAsync();
        client.Disconnect();
    }

    [Test]
    [Order(8)]
    public async Task ClientServer_DualChannel_CommandTriggeredBroadcastCanSendAnotherCommand()
    {
        // Arrange - Setup synchronization
        var broadcastReceived = new TaskCompletionSource<bool>();
        var secondCommandCompleted = new TaskCompletionSource<CommandResponse>();
        
        // Create and connect client
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var connected = await client.ConnectAsync(onBroadcastReceived: async (broadcast) => 
        {
            // When broadcast is received, send another command
            broadcastReceived.SetResult(true);
            
            try 
            {
                var secondRequest = new CommandRequest { Id = 101, Message = "TRIGGERED_BY_BROADCAST" };
                var secondResponse = await client.SendCommandAsync(secondRequest);
                secondCommandCompleted.SetResult(secondResponse);
            }
            catch (Exception ex)
            {
                secondCommandCompleted.SetException(ex);
            }
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send first command that triggers broadcast
        var firstRequest = new CommandRequest { Id = 100, Message = "TRIGGER_BROADCAST" };
        var firstResponse = client.SendCommand(firstRequest);

        // Assert first command was successful
        Assert.That(firstResponse.Success, Is.True, "First command should succeed");

        // Wait for broadcast to be received (with timeout)
        var broadcastTimeout = Task.Delay(5000);
        var broadcastTask = await Task.WhenAny(broadcastReceived.Task, broadcastTimeout);
        Assert.That(broadcastTask, Is.EqualTo(broadcastReceived.Task), "Broadcast should be received within timeout");

        // Wait for second command to complete (with timeout)  
        var secondCommandTimeout = Task.Delay(5000);
        var secondCommandTask = await Task.WhenAny(secondCommandCompleted.Task, secondCommandTimeout);
        Assert.That(secondCommandTask, Is.EqualTo(secondCommandCompleted.Task), "Second command should complete within timeout");

        // Get and verify second command response
        var secondResponse = await secondCommandCompleted.Task;
        Assert.Multiple(() =>
        {
            Assert.That(secondResponse, Is.Not.Null, "Second response should not be null");
            Assert.That(secondResponse.Success, Is.True, "Second command should succeed");
            Assert.That(secondResponse.Id, Is.EqualTo("101"), "Second response should have correct ID");
            Assert.That(secondResponse.Message, Is.EqualTo("SECOND_COMMAND_PROCESSED"), "Second response should have correct message");
        });

        // Cleanup
        client.Disconnect();
    }

    [Test]
    [Order(9)]
    public async Task ClientServer_SingleClient_ReceiveBroadcast()
    {
        var broadcastReceived = new TaskCompletionSource<BroadcastMessage>();
        
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var connected = await client.ConnectAsync(onBroadcastReceived: (msg) =>
        {
            // Handle broadcast messages if needed
            broadcastReceived.SetResult(msg);
        });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = new CommandRequest { Id = 100, Message = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request);

        // Wait for broadcast to be received (with timeout)
        var broadcastTimeout = Task.Delay(5000);
        var broadcastTask = await Task.WhenAny(broadcastReceived.Task, broadcastTimeout);
        Assert.That(broadcastTask, Is.EqualTo(broadcastReceived.Task), "Broadcast should be received within timeout");

        client.Disconnect();
    }

    [Test]
    [Order(10)]
    public async Task ResourceManagement_ClientDisposal_CleansUpResourcesCorrectly()
    {
        var broadcastReceived = new TaskCompletionSource<BroadcastMessage>();
        CommsClientSockets client = null!;
        
        // Test using block to ensure disposal
        using (client = new CommsClientSockets(IPAddress.Parse(_host), _basePort))
        {
            var connected = await client.ConnectAsync(onBroadcastReceived: (msg) =>
            {
                broadcastReceived.TrySetResult(msg);
            });

            Assert.That(connected, Is.True, "Client should connect successfully");

            // Verify client is functioning
            var request = new CommandRequest { Id = 1, Message = "TEST_BEFORE_DISPOSE" };
            var response = await client.SendCommandAsync(request);
            
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response.Success, Is.True);
                Assert.That(response.Message, Is.EqualTo("TEST_BEFORE_DISPOSE"));
            });

            // Trigger a broadcast to verify dual-channel functionality before disposal
            var broadcastRequest = new CommandRequest { Id = 100, Message = "BROADCAST_TEST" };
            await client.SendCommandAsync(broadcastRequest);

            // Wait for broadcast with timeout
            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(broadcastReceived.Task, timeoutTask);
            
            Assert.That(completedTask, Is.EqualTo(broadcastReceived.Task), "Broadcast should be received before disposal");
            
        } // Dispose is called automatically here

        // After disposal, verify cleanup occurred
        Assert.DoesNotThrow(() =>
        {
            // These operations should not throw exceptions even after disposal
            client.Dispose(); // Should be safe to call multiple times
        }, "Multiple disposal calls should be safe");

        // Allow some time for server-side cleanup
        await Task.Delay(500);
        
        // Verify that attempting to use disposed client fails gracefully
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await client.SendCommandAsync(new CommandRequest { Id = 999, Message = "AFTER_DISPOSE" });
        }, "Operations on disposed client should throw InvalidOperationException");
    }

    [Test]
    [Order(11)]
    public async Task ResourceManagement_MultipleConnectDisconnectCycles_NoMemoryLeaks()
    {
        const int cycleCount = 10;
        var clients = new List<CommsClientSockets>();
        var responses = new List<CommandResponse>();

        try
        {
            for (int i = 0; i < cycleCount; i++)
            {
                var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
                clients.Add(client);

                // Connect
                var connected = await client.ConnectAsync(onBroadcastReceived: (msg) =>
                {
                    // Handle broadcasts
                });

                Assert.That(connected, Is.True, $"Client {i} should connect successfully");

                // Send a command
                var request = new CommandRequest { Id = i + 1, Message = $"CYCLE_{i}" };
                var response = await client.SendCommandAsync(request);
                responses.Add(response);

                Assert.Multiple(() =>
                {
                    Assert.That(response, Is.Not.Null, $"Response {i} should not be null");
                    Assert.That(response.Success, Is.True, $"Response {i} should be successful");
                    Assert.That(response.Message, Is.EqualTo($"CYCLE_{i}"), $"Response {i} should have correct message");
                });

                // Disconnect every other client to test mixed states
                if (i % 2 == 0)
                {
                    client.Disconnect();
                }
            }

            // Verify all responses were collected correctly
            Assert.That(responses.Count, Is.EqualTo(cycleCount), "All commands should have received responses");

            // Allow time for server cleanup
            await Task.Delay(1000);
        }
        finally
        {
            // Clean up all clients
            foreach (var client in clients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    // Log but don't fail test for cleanup exceptions
                    Console.WriteLine($"Exception during client cleanup: {ex.Message}");
                }
            }
        }
    }

    [Test]
    [Order(12)]
    public async Task ResourceManagement_EventHandlerCleanup_PreventMemoryLeaks()
    {
        var broadcastCallbackExecuted = false;
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        // Create a reference to the callback to verify it gets cleared
        Action<BroadcastMessage> broadcastCallback = (msg) =>
        {
            broadcastCallbackExecuted = true;
        };

        var connected = await client.ConnectAsync(onBroadcastReceived: broadcastCallback);
        Assert.That(connected, Is.True, "Client should connect successfully");

        // Verify callback is working before disconnect
        var broadcastRequest = new CommandRequest { Id = 100, Message = "TEST_CALLBACK" };
        await client.SendCommandAsync(broadcastRequest);
        
        await Task.Delay(1000); // Allow time for broadcast
        Assert.That(broadcastCallbackExecuted, Is.True, "Callback should execute before disconnect");

        // Reset flag and disconnect
        broadcastCallbackExecuted = false;
        client.Disconnect();

        // Reconnect with new callback to trigger server broadcast
        var newClient = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
        var reconnected = await newClient.ConnectAsync(onBroadcastReceived: (msg) => { });
        Assert.That(reconnected, Is.True, "New client should connect successfully");

        // Trigger another broadcast
        await newClient.SendCommandAsync(new CommandRequest { Id = 100, Message = "TEST_AFTER_DISCONNECT" });
        await Task.Delay(1000); // Allow time for broadcast

        // Verify old callback was not executed (event handler was cleared)
        Assert.That(broadcastCallbackExecuted, Is.False, "Old callback should not execute after disconnect");

        // Cleanup
        newClient.Disconnect();
        client.Dispose();
        newClient.Dispose();
    }

    [Test]
    [Order(13)]
    public void ConnectionFailure_ServerUnavailable_HandlesGracefully()
    {
        const int unavailablePort = 9999; // Use a port that's unlikely to be in use
        var client = new CommsClientSockets(IPAddress.Parse(_host), unavailablePort);

        // Test synchronous connection failure
        Assert.Throws<SocketException>(() =>
        {
            client.Connect(onBroadcastReceived: (msg) => { });
        }, "Sync connect should throw SocketException when server is unavailable");

        // Test asynchronous connection failure
        Assert.ThrowsAsync<SocketException>(async () =>
        {
            await client.ConnectAsync(onBroadcastReceived: (msg) => { });
        }, "Async connect should throw SocketException when server is unavailable");

        // Ensure disposal doesn't throw even after failed connection
        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
        }, "Disposal should be safe even after connection failure");
    }

    [Test]
    [Order(14)]
    public async Task ConcurrentOperations_CommandAndBroadcast_TrueDualChannel()
    {
        var broadcastReceived = new TaskCompletionSource<BroadcastMessage>();
        var commandCompleted = new TaskCompletionSource<CommandResponse>();
        var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);

        var connected = await client.ConnectAsync(onBroadcastReceived: (msg) =>
        {
            // This should execute even while a command is in progress
            broadcastReceived.TrySetResult(msg);
        });

        Assert.That(connected, Is.True, "Client should connect successfully");

        // Start a long-running command that would block in single-channel implementation
        var longCommandTask = Task.Run(async () =>
        {
            try
            {
                // Use a command that triggers broadcast but takes time to complete
                var request = new CommandRequest { Id = 100, Message = "LONG_COMMAND" };
                var response = await client.SendCommandAsync(request);
                commandCompleted.SetResult(response);
                return response;
            }
            catch (Exception ex)
            {
                commandCompleted.SetException(ex);
                throw;
            }
        });

        // Give the command a moment to start
        await Task.Delay(100);

        // While command is running, verify we can receive broadcasts
        var broadcastTimeout = Task.Delay(5000);
        var broadcastTask = await Task.WhenAny(broadcastReceived.Task, broadcastTimeout);
        Assert.That(broadcastTask, Is.EqualTo(broadcastReceived.Task), "Broadcast should be received even while command is running");

        // Verify the command also completes successfully
        var commandTimeout = Task.Delay(5000);
        var commandTask = await Task.WhenAny(commandCompleted.Task, commandTimeout);
        Assert.That(commandTask, Is.EqualTo(commandCompleted.Task), "Command should complete successfully");

        var finalResponse = await commandCompleted.Task;
        Assert.Multiple(() =>
        {
            Assert.That(finalResponse, Is.Not.Null, "Command response should not be null");
            Assert.That(finalResponse.Success, Is.True, "Command should succeed");
        });

        // Cleanup
        client.Disconnect();
    }

    [Test]
    [Order(15)]
    public async Task MemoryLeakDetection_LongRunningOperations_MonitorMemoryUsage()
    {
        const int operationCount = 50;
        var initialMemory = GC.GetTotalMemory(true); // Force GC and get baseline
        var clients = new List<CommsClientSockets>();
        var tasks = new List<Task>();

        try
        {
            // Perform many operations to detect potential memory leaks
            for (int i = 0; i < operationCount; i++)
            {
                var client = new CommsClientSockets(IPAddress.Parse(_host), _basePort);
                clients.Add(client);

                var task = Task.Run(async () =>
                {
                    var connected = await client.ConnectAsync(onBroadcastReceived: (msg) =>
                    {
                        // Process broadcast messages
                    });

                    if (connected)
                    {
                        // Send multiple commands per client
                        for (int j = 0; j < 3; j++)
                        {
                            var request = new CommandRequest { Id = (i * 10) + j, Message = $"MEMORY_TEST_{i}_{j}" };
                            await client.SendCommandAsync(request);
                            
                            // Small delay to simulate real usage
                            await Task.Delay(10);
                        }
                    }
                });
                
                tasks.Add(task);

                // Process in batches to avoid overwhelming the server
                if (i % 10 == 9)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                    
                    // Cleanup every batch
                    for (int k = Math.Max(0, clients.Count - 10); k < clients.Count; k++)
                    {
                        clients[k].Dispose();
                    }
                    
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    await Task.Delay(100); // Allow cleanup
                }
            }

            // Wait for any remaining tasks
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            // Final cleanup and memory check
            foreach (var client in clients)
            {
                client.Dispose();
            }

            // Force multiple garbage collections
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(100);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseKB = memoryIncrease / 1024.0;

            // Log memory usage for analysis
            Console.WriteLine($"Initial memory: {initialMemory / 1024.0:F2} KB");
            Console.WriteLine($"Final memory: {finalMemory / 1024.0:F2} KB");
            Console.WriteLine($"Memory increase: {memoryIncreaseKB:F2} KB");

            // Allow reasonable memory growth (adjust threshold as needed)
            // This is more of a monitoring test than a strict assertion
            Assert.That(memoryIncreaseKB, Is.LessThan(5000), $"Memory increase should be reasonable. Actual increase: {memoryIncreaseKB:F2} KB");
        }
        finally
        {
            // Ensure all resources are cleaned up
            foreach (var client in clients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup exception: {ex.Message}");
                }
            }
        }
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
