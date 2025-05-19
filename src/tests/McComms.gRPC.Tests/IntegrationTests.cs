using NUnit.Framework;
using System.Collections.Concurrent;
using McComms.Core;
using System.Diagnostics;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class IntegrationTests
{
    // Base port number - each test will use BasePort + test order
    private const int BasePort = 9000;
    private int GetTestPort(int testOrder) => BasePort + testOrder;
    
    private string LoopbackAddress = "127.0.0.1";
    private CancellationTokenSource _serverCts = null!;
        

    [SetUp]
    public void Setup()
    {
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
        var server = new CommsServerGrpc(LoopbackAddress, testPort);
        
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
        var client = new CommsClientGrpc(LoopbackAddress, testPort);
        var connected = client.Connect(null);

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
            Assert.That(response.Message, Is.EqualTo("Echo: TEST_COMMAND"));
        });

        // Cleanup
        client.Disconnect();
        server.Stop();
    }

    [Test]
    [Order(2)]
    public void ClientServer_MultipleClients_AllReceiveResponses()
    {
        // Arrange
        int testPort = GetTestPort(2);
        var server = new CommsServerGrpc(LoopbackAddress, testPort);
        
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
        var client1 = new CommsClientGrpc(LoopbackAddress, testPort);
        var client2 = new CommsClientGrpc(LoopbackAddress, testPort);
        
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
    [Order(3)]
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast()
    {
        // Arrange
        int testPort = GetTestPort(3);
        var server = new CommsServerGrpc(LoopbackAddress, testPort);
        
        // Start server
        server.Start(request => new CommandResponse { Success = true }, _serverCts.Token);

        // Setup broadcast message collection for each client
        var client1ReceivedMessages = new ConcurrentBag<BroadcastMessage>();
        var client2ReceivedMessages = new ConcurrentBag<BroadcastMessage>();

        // Create and connect clients with broadcast handlers
        var client1 = new CommsClientGrpc(LoopbackAddress, testPort);
        var client2 = new CommsClientGrpc(LoopbackAddress, testPort);
        
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
        Thread.Sleep(3000);

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
    [Order(4)]
    public async Task ServerClientScenario_ComplexInteraction_WorksAsExpected()
    {
        // Arrange
        int testPort = GetTestPort(4);
        var server = new CommsServerGrpc(LoopbackAddress, testPort);
        
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
        var client1 = new CommsClientGrpc(LoopbackAddress, testPort);
        var client2 = new CommsClientGrpc(LoopbackAddress, testPort);
        
        var connected1 = client1.Connect(msg => receivedBroadcasts.Add(msg));
        var connected2 = client2.Connect(msg => receivedBroadcasts.Add(msg));

        // Act - Multiple interactions
        
        // 1. Client 1 sends status request
        var statusRequest = new CommandRequest { Id = 0 };
        var statusResponse = client1.SendCommand(statusRequest);
        
        // 2. Client 2 sends data processing request
        var dataRequest = new CommandRequest { Id = 1, Message = "SAMPLE123" };
        var dataResponse = client2.SendCommand(dataRequest);
        
        // 3. Server broadcasts an update
        var updateBroadcast = new BroadcastMessage { Id = 5, Message = "System updated to version 2.0" };
        server.SendBroadcast(updateBroadcast);
        
        // Give time for the broadcast to be received
        await Task.Delay(500);
        
        // 4. Client 1 sends an invalid command
        var invalidRequest = new CommandRequest { Id = 3 };
        var invalidResponse = client1.SendCommand(invalidRequest);

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

    [Test]
    [Order(5)]
    public async Task ServerClientInSeparateThreads_CommunicateSuccessfully()
    {
        // Arrange
        int testPort = GetTestPort(5);
        var serverCompletionSource = new TaskCompletionSource<bool>();
        var clientCompletionSource = new TaskCompletionSource<CommandResponse>();
        var serverExceptionSource = new TaskCompletionSource<Exception>();
        var clientExceptionSource = new TaskCompletionSource<Exception>();
        string testMessage = "TEST_SEPARATE_THREADS";
        
        // Start server in separate thread
        var serverTask = Task.Run(async () =>
        {
            try
            {
                var server = new CommsServerGrpc(LoopbackAddress, testPort);
                
                // Start server with command handler
                server.Start(request => 
                {
                    // Process the request and return response
                    var response = new CommandResponse 
                    { 
                        Success = true, 
                        Message = $"Echo from thread: {request.Message}" 
                    };
                    
                    serverCompletionSource.SetResult(true);
                    return response;
                }, _serverCts.Token);
                
                // Keep server running until test is done
                await Task.Delay(10000, _serverCts.Token)
                    .ContinueWith(t => { /* Ignore if cancelled */ }, TaskContinuationOptions.OnlyOnCanceled);
                
                // Cleanup
                server.Stop();
            }
            catch (Exception ex)
            {
                serverExceptionSource.SetResult(ex);
                throw;
            }
        });
        
        // Give server time to start up
        await Task.Delay(500);
        
        // Start client in separate thread
        var clientTask = Task.Run(async () =>
        {
            try
            {
                var client = new CommsClientGrpc(LoopbackAddress, testPort);
                var connected = client.Connect(null);
                
                // Make sure we're connected
                if (!connected)
                {
                    clientCompletionSource.SetException(new Exception("Failed to connect to server"));
                    return;
                }
                
                // Send a command and store response for verification
                var request = new CommandRequest { Id = 5, Message = testMessage };
                var response = client.SendCommand(request);
                clientCompletionSource.SetResult(response);
                
                // Wait until the test is done
                await Task.Delay(8000, _serverCts.Token)
                    .ContinueWith(t => { /* Ignore if cancelled */ }, TaskContinuationOptions.OnlyOnCanceled);
                
                // Cleanup
                client.Disconnect();
            }
            catch (Exception ex)
            {
                clientExceptionSource.SetResult(ex);
                throw;
            }
        });
        
        // Wait for both server and client to finish their initial processing
        await Task.WhenAny(
            serverCompletionSource.Task,
            serverExceptionSource.Task,
            clientCompletionSource.Task,
            clientExceptionSource.Task,
            Task.Delay(5000) // Safety timeout
        );
        
        // Check for exceptions
        if (serverExceptionSource.Task.IsCompleted)
        {
            Assert.Fail($"Server exception: {serverExceptionSource.Task.Result}");
        }
        
        if (clientExceptionSource.Task.IsCompleted)
        {
            Assert.Fail($"Client exception: {clientExceptionSource.Task.Result}");
        }
        
        // Verify client received response correctly
        Assert.That(clientCompletionSource.Task.IsCompleted, Is.True, "Client did not complete request");
        
        var clientResponse = clientCompletionSource.Task.Result;
        Assert.That(clientResponse.Success, Is.True);
        Assert.That(clientResponse.Message, Is.EqualTo($"Echo from thread: {testMessage}"));
        
        // Cancel the server and cleanup
        _serverCts.Cancel();
          // Wait for both tasks to finish with a timeout
        var completionTask = Task.WhenAll(
            serverTask.ContinueWith(t => { /* Ignore exceptions */ }),
            clientTask.ContinueWith(t => { /* Ignore exceptions */ })
        );
        
        // Use a timeout but don't await the result of Wait()
        var completed = Task.WaitAll(new[] { completionTask }, 2000);
    }

    [Test]
    [Order(6)]
    public async Task ParallelServerClientExecution_UsingTaskFactory_CommunicateSuccessfully()
    {
        // Arrange
        int testPort = GetTestPort(6);
        string testMessage = "TEST_PARALLEL_EXECUTION";
        var serverReady = new ManualResetEventSlim(false);
        var clientResult = new ConcurrentBag<CommandResponse>();
        
        // Create a cancellation token that will be used to stop both server and client
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        
        try
        {
            // Create server task
            Task serverTask = Task.Factory.StartNew(() =>
            {
                // Server setup
                var server = new CommsServerGrpc(LoopbackAddress, testPort);
                
                // Start server with command handler
                server.Start(request =>
                {
                    var response = new CommandResponse
                    {
                        Success = true,
                        Message = $"Server processed: {request.Message}"
                    };
                    return response;
                }, token);
                
                // Signal that server is ready
                serverReady.Set();
                
                try
                {
                    // Keep the server running until cancellation
                    token.WaitHandle.WaitOne();
                }
                finally
                {
                    // Ensure server is stopped on method exit
                    server.Stop();
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
            // Wait for server to be ready
            Assert.That(serverReady.Wait(TimeSpan.FromSeconds(5)), Is.True, "Server failed to start within timeout");
            
            // Create client task
            Task clientTask = Task.Factory.StartNew(() =>
            {
                // Client setup
                var client = new CommsClientGrpc(LoopbackAddress, testPort);
                var connected = client.Connect(null);
                
                Assert.That(connected, Is.True, "Client failed to connect");
                
                try
                {
                    // Send command
                    var request = new CommandRequest { Id = 10, Message = testMessage };
                    var response = client.SendCommand(request);
                    clientResult.Add(response);
                }
                finally
                {
                    // Ensure client is disconnected on method exit
                    client.Disconnect();
                }
            }, token, TaskCreationOptions.None, TaskScheduler.Default);
            
            // Wait for client task to complete
            Assert.That(clientTask.Wait(TimeSpan.FromSeconds(5)), Is.True, "Client task did not complete within timeout");
            
            // Verify results
            Assert.That(clientResult, Has.Count.EqualTo(1));
            var response = clientResult.First();
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo($"Server processed: {testMessage}"));
        }
        finally
        {
            // Ensure both tasks are cancelled and disposed
            cts.Cancel();
            cts.Dispose();
            
            // Wait for server task to finish (with timeout)
            // This is important to ensure resources are properly released
            await Task.Delay(1000);
        }
    }

}
