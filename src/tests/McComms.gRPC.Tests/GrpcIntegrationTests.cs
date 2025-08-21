using Commsproto;
using Grpc.Core;
using McComms.Core;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace McComms.gRPC.Tests;

[TestFixture]
[NonParallelizable]
public class GrpcIntegrationTests {
    // Base port number - each test will use BasePort + test order
    private const int _basePort = 9000;
    private int GetTestPort(int testOrder) => _basePort + testOrder;

    private readonly string _host = "127.0.0.1";
    //private CancellationTokenSource _serverCts = null!;

    private readonly ServerCredentials _serverCredentials = ServerCredentials.Insecure;

    private GrpcServer _server = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup() {
        var commsHost = new CommsAddress(_host, _basePort);
        _server = new GrpcServer(commsHost: commsHost, credentials: _serverCredentials);
        _server.Start(onCommandReceived: (request) => {
            if (request.Id == 100) {
                // Simulate a broadcast message
                var broadcastMessage = new mcBroadcast { Id = 100, Content = "TEST_BROADCAST" };
                _server.SendBroadcast(broadcastMessage);
            }
            else if (request.Id == 200) {
                return new mcCommandResponse { Success = true, Message = GenerateRandomLongString() };
            }
            else if (request.Id == 300) {
                Thread.Sleep(2000);
            }
            return new mcCommandResponse { Success = true, Message = request.Content };
        });
        await Task.Delay(2000); // Give server time to start
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        _server.Stop();
    }

    [Test]
    [Order(1)]
    public void ClientServer_SingleClient_SendCommandReceivesResponse() {
        // Create and connect client
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = client.Connect(onBroadcastReceived: (msg) => {
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(2)]
    public async Task ClientServer_SingleClient_SendCommandAsyncReceivesResponse() {
        // Create and connect client
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync(onBroadcastReceived: (msg) => {
            // Handle broadcast messages if needed
        });

        // Assert client connected successfully
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a command
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request);

        // Assert
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(3)]
    public void ClientServer_MultipleClients_AllReceiveResponses() {
        // Create and connect multiple clients
        var commsHost = new CommsAddress(_host, _basePort);
        var client1 = new GrpcClient(commsHost);
        var client2 = new GrpcClient(commsHost);

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

        Assert.Multiple(() => {
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
    public async Task ClientServer_MultipleClientsAsync_AllReceiveResponses() {
        // Create and connect multiple clients
        var commsHost = new CommsAddress(_host, _basePort);
        var client1 = new GrpcClient(commsHost);
        var client2 = new GrpcClient(commsHost);

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

        Assert.Multiple(() => {
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
    public void BroadcastMessage_MultipleConnectedClients_AllReceiveBroadcast() {
        // Setup broadcast message collection for each client
        var client1ReceivedMessages = new ConcurrentBag<mcBroadcast>();
        var client2ReceivedMessages = new ConcurrentBag<mcBroadcast>();

        // Create and connect clients with broadcast handlers
        var commsHost = new CommsAddress(_host, _basePort);
        var client1 = new GrpcClient(commsHost);
        var client2 = new GrpcClient(commsHost);

        var connected1 = client1.Connect(msg => client1ReceivedMessages.Add(msg));
        var connected2 = client2.Connect(msg => client2ReceivedMessages.Add(msg));

        Assert.Multiple(() => {
            // Assert clients connected successfully
            Assert.That(connected1, Is.True, "Client 1 failed to connect to server");
            Assert.That(connected2, Is.True, "Client 2 failed to connect to server");
        });

        // Initiate broadcast
        client1.SendCommand(new mcCommandRequest { Id = 100, Content = "BROADCAST_TEST" });

        // Wait a bit for the broadcast to be received
        Thread.Sleep(5000);

        Assert.Multiple(() => {
            // Assert
            Assert.That(client1ReceivedMessages, Is.Not.Empty);
            Assert.That(client2ReceivedMessages, Is.Not.Empty);
        });

        var client1Broadcast = client1ReceivedMessages.First();
        var client2Broadcast = client2ReceivedMessages.First();

        Assert.Multiple(() => {
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
    public void ClientServer_Clients_ReceiveLongResponse() {
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);

        var connected = client.Connect((_) => { });

        Assert.That(connected, Is.True, "Client failed to connect to server");

        var request = new mcCommandRequest { Id = 200, Content = "" };

        var response = client.SendCommand(request);

        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message.Length, Is.EqualTo(2048));
        });

        client.Disconnect();
    }

    [Test]
    [Order(6)]
    public void SendCommand_WithVeryShortTimeout_ReturnsErrorResponse() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        client.Timeout = 0; // Set extremely short timeout (0 seconds)

        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act
        var request = new mcCommandRequest { Id = 300, Content = "TIMEOUT_TEST" };
        var response = client.SendCommand(request);

        // Assert - Should return error response due to timeout
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Communication error"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(7)]
    public async Task SendCommandAsync_WithVeryShortTimeout_ReturnsErrorResponse() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        client.Timeout = 0; // Set extremely short timeout (0 seconds)

        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act
        var request = new mcCommandRequest { Id = 300, Content = "TIMEOUT_TEST" };
        var response = await client.SendCommandAsync(request);

        // Assert - Should return error response due to timeout
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Communication error"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(8)]
    public async Task SendCommandAsync_WithCancellationToken_CancelsOperation() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Create a cancellation token that cancels immediately
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request, cts.Token);

        // Should return error response due to cancellation
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Communication error"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(9)]
    public async Task SendCommandAsync_WithDelayedCancellation_CompletesBeforeCancellation() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Create a cancellation token that cancels after 5 seconds (should be enough for command to complete)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request, cts.Token);

        // Assert - Should complete successfully before cancellation
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(10)]
    public async Task ConnectAsync_WithCancellationToken_CancelsConnection() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);

        // Create a cancellation token that cancels immediately
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var connected = await client.ConnectAsync((_) => { }, cts.Token);

        // Assert - Connection should fail due to cancellation
        Assert.That(connected, Is.False, "Connection should have been cancelled");
    }

    [Test]
    [Order(11)]
    public void SendCommand_WithCustomTimeout_RespectsTimeoutSetting() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        client.Timeout = 1; // Set 1 second timeout

        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send a normal command that should complete within timeout
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert - Should complete successfully within timeout
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("TEST_COMMAND"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(12)]
    public async Task SendCommandAsync_StressTest_MultipleSimultaneousRequests() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send multiple commands simultaneously
        var tasks = new List<Task<mcCommandResponse>>();
        for (int i = 0; i < 10; i++) {
            var request = new mcCommandRequest { Id = i, Content = $"STRESS_TEST_{i}" };
            tasks.Add(client.SendCommandAsync(request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete successfully
        Assert.That(responses.Length, Is.EqualTo(10));

        for (int i = 0; i < responses.Length; i++) {
            Assert.Multiple(() => {
                Assert.That(responses[i], Is.Not.Null, $"Response {i} should not be null");
                Assert.That(responses[i].Success, Is.True, $"Response {i} should be successful");
                Assert.That(responses[i].Message, Is.EqualTo($"STRESS_TEST_{i}"), $"Response {i} message should match");
            });
        }

        await client.DisconnectAsync();
    }

    [Test]
    [Order(13)]
    public void Connect_ToNonExistentServer_ReturnsFalse() {
        var nonExistentPort = 65432;
        var commsHost = new CommsAddress(_host, nonExistentPort);
        var client = new GrpcClient(commsHost);

        var connected = client.Connect((_) => { });

        Assert.That(connected, Is.False, "Should not be able to connect to non-existent server");

        client.Disconnect();
    }

    [Test]
    [Order(14)]
    public async Task ConnectAsync_ToNonExistentServer_ReturnsFalse() {
        var nonExistentPort = 65433;
        var commsHost = new CommsAddress(_host, nonExistentPort);
        var client = new GrpcClient(commsHost);

        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.False, "Should not be able to connect to non-existent server");

        // Cleanup attempt (should be safe even if not connected)
        await client.DisconnectAsync();
    }

    [Test]
    [Order(15)]
    public void Connect_ToInvalidHost_ReturnsFalse() {
        // Arrange - Use an invalid hostname
        var invalidHost = "invalid.hostname.that.does.not.exist";
        var commsHost = new CommsAddress(invalidHost, _basePort);
        var client = new GrpcClient(commsHost);

        // Act
        var connected = client.Connect((_) => { });

        // Assert - Should fail to connect
        Assert.That(connected, Is.False, "Should not be able to connect to invalid host");

        // Cleanup attempt (should be safe even if not connected)
        client.Disconnect();
    }

    [Test]
    [Order(16)]
    public void SendCommand_ToNonExistentServer_ReturnsErrorResponse() {
        // Arrange - Create client connected to non-existent server
        var nonExistentPort = 65434;
        var commsHost = new CommsAddress(_host, nonExistentPort);
        var client = new GrpcClient(commsHost);

        // Act - Try to send command without connecting (or to non-existent server)
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = client.SendCommand(request);

        // Assert - Should return error response
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Communication error"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(17)]
    public async Task SendCommandAsync_ToNonExistentServer_ReturnsErrorResponse() {
        // Arrange - Create client connected to non-existent server
        var nonExistentPort = 65435;
        var commsHost = new CommsAddress(_host, nonExistentPort);
        var client = new GrpcClient(commsHost);

        // Act - Try to send command without connecting (or to non-existent server)
        var request = new mcCommandRequest { Id = 1, Content = "TEST_COMMAND" };
        var response = await client.SendCommandAsync(request);

        // Assert - Should return error response
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Communication error"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(18)]
    public async Task SendCommand_AfterServerShutdown_HandlesGracefully() {
        // Arrange - Create a temporary server on a different port
        var tempPort = _basePort + 100;
        var commsHost = new CommsAddress(_host, tempPort);
        var tempServer = new GrpcServer(commsHost, _serverCredentials);
        tempServer.Start(onCommandReceived: (request) => {
            return new mcCommandResponse { Success = true, Message = request.Content };
        });

        // Give server time to start
        await Task.Delay(1000);
    
        var client = new GrpcClient(commsHost);
        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Should connect to temporary server");

        // Send a successful command first
        var request1 = new mcCommandRequest { Id = 1, Content = "BEFORE_SHUTDOWN" };
        var response1 = client.SendCommand(request1);
        Assert.That(response1.Success, Is.True, "First command should succeed");

        // Act - Shutdown the server while client is still connected
        tempServer.Stop();
        await Task.Delay(1000); // Give time for shutdown

        // Try to send command after server shutdown
        var request2 = new mcCommandRequest { Id = 2, Content = "AFTER_SHUTDOWN" };
        var response2 = client.SendCommand(request2);

        // Assert - Should handle shutdown gracefully with error response
        Assert.Multiple(() => {
            Assert.That(response2, Is.Not.Null);
            Assert.That(response2.Success, Is.False);
            Assert.That(response2.Message, Does.Contain("Communication error"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(19)]
    public async Task BroadcastListener_AfterServerShutdown_HandlesGracefully() {
        // Arrange - Create a temporary server on a different port
        var tempPort = _basePort + 101;
        var commsHost = new CommsAddress(_host, tempPort);
        var tempServer = new GrpcServer(commsHost, _serverCredentials);

        var broadcastReceived = false;
        var broadcastMessages = new ConcurrentBag<mcBroadcast>();

        tempServer.Start(onCommandReceived: (request) => {
            if (request.Id == 999) {
                var broadcastMessage = new mcBroadcast { Id = 999, Content = "SHUTDOWN_TEST" };
                tempServer.SendBroadcast(broadcastMessage);
            }
            return new mcCommandResponse { Success = true, Message = request.Content };
        });

        // Give server time to start
        await Task.Delay(1000);

        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync(msg => {
            broadcastMessages.Add(msg);
            broadcastReceived = true;
        });
        Assert.That(connected, Is.True, "Should connect to temporary server");

        // Send a command that triggers a broadcast
        var request1 = new mcCommandRequest { Id = 999, Content = "TRIGGER_BROADCAST" };
        var response1 = await client.SendCommandAsync(request1);
        Assert.That(response1.Success, Is.True, "Command should succeed");

        // Wait for broadcast
        await Task.Delay(2000);
        Assert.That(broadcastReceived, Is.True, "Should have received broadcast before shutdown");

        // Act - Shutdown the server
        tempServer.Stop();
        await Task.Delay(2000); // Give time for shutdown and client to detect it

        // The client should handle the server shutdown gracefully
        // (No exceptions should be thrown, client should detect disconnection)

        await client.DisconnectAsync();
        
        // Assert - Should have received at least one broadcast before shutdown
        Assert.That(broadcastMessages, Is.Not.Empty, "Should have received broadcast messages");
    }

    [Test]
    [Order(20)]
    public void Connect_WithInvalidPort_ReturnsFalse() {
        // Arrange - Use an invalid port number (negative or too high)
        var invalidPort = -1;

        // Act & Assert - Should handle invalid port gracefully
        Assert.DoesNotThrow((TestDelegate)(() => {
            var commsHost = new CommsAddress(_host, invalidPort);
            var client = new GrpcClient(commsHost);
            var connected = client.Connect((_) => { });
            Assert.That(connected, Is.False, "Should not connect with invalid port");
            client.Disconnect();
        }));
    }

    [Test]
    [Order(21)]
    public async Task MultipleClients_ServerShutdown_AllHandleGracefully() {
        // Arrange - Create a temporary server
        var tempPort = _basePort + 102;
        var commsHost = new CommsAddress(_host, tempPort);
        var tempServer = new GrpcServer(commsHost, _serverCredentials);
        tempServer.Start(onCommandReceived: (request) => {
            return new mcCommandResponse { Success = true, Message = request.Content };
        });

        await Task.Delay(1000);

        // Connect multiple clients
        var client1 = new GrpcClient(commsHost);
        var client2 = new GrpcClient(commsHost);
        var client3 = new GrpcClient(commsHost);

        var connected1 = await client1.ConnectAsync((_) => { });
        var connected2 = await client2.ConnectAsync((_) => { });
        var connected3 = await client3.ConnectAsync((_) => { });

        Assert.Multiple(() => {
            Assert.That(connected1, Is.True, "Client 1 should connect");
            Assert.That(connected2, Is.True, "Client 2 should connect");
            Assert.That(connected3, Is.True, "Client 3 should connect");
        });

        // Act - Shutdown server while multiple clients are connected
        tempServer.Stop();
        await Task.Delay(1000);

        // All clients should handle the shutdown gracefully
        var request = new mcCommandRequest { Id = 1, Content = "AFTER_SHUTDOWN" };
        
        var response1 = await client1.SendCommandAsync(request);
        var response2 = await client2.SendCommandAsync(request);
        var response3 = await client3.SendCommandAsync(request);

        // Assert - All should return error responses, not throw exceptions
        Assert.Multiple(() => {
            Assert.That(response1.Success, Is.False, "Client 1 should get error response");
            Assert.That(response2.Success, Is.False, "Client 2 should get error response");
            Assert.That(response3.Success, Is.False, "Client 3 should get error response");
        });

        await client1.DisconnectAsync();
        await client2.DisconnectAsync();
        await client3.DisconnectAsync();
    }

    [Test]
    [Order(23)]
    public async Task SendCommandAsync_WithEmptyContent_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with empty content
        var request = new mcCommandRequest { Id = 1, Content = "" };
        var response = await client.SendCommandAsync(request);

        // Assert - Should handle empty content gracefully
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("")); // Should echo empty string
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(24)]
    public void SendCommand_WithExtremelyLongContent_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Create extremely long content (1MB string)
        var longContent = new string('A', 1024 * 1024);

        // Act - Send command with very long content
        var request = new mcCommandRequest { Id = 1, Content = longContent };
        var response = client.SendCommand(request);

        // Assert - Should handle large content
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            // Might succeed or fail depending on gRPC message size limits
            if (response.Success) {
                Assert.That(response.Message, Is.EqualTo(longContent));
            } else {
                Assert.That(response.Message, Does.Contain("Communication error"));
            }
        });

        client.Disconnect();
    }

    [Test]
    [Order(25)]
    public async Task SendCommand_WithSpecialCharacters_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with special characters
        var specialContent = "Special chars: éñüñ😀🚀\n\t\r\\\"'<>&";
        var request = new mcCommandRequest { Id = 1, Content = specialContent };
        var response = await client.SendCommandAsync(request);

        // Assert - Should handle special characters
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo(specialContent));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(26)]
    public void SendCommand_WithNegativeId_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with negative ID
        var request = new mcCommandRequest { Id = -999, Content = "NEGATIVE_ID_TEST" };
        var response = client.SendCommand(request);

        // Assert - Should handle negative ID gracefully
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("NEGATIVE_ID_TEST"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(27)]
    public async Task SendCommand_WithZeroId_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with zero ID
        var request = new mcCommandRequest { Id = 0, Content = "ZERO_ID_TEST" };
        var response = await client.SendCommandAsync(request);

        // Assert - Should handle zero ID gracefully
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("ZERO_ID_TEST"));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(28)]
    public async Task SendCommand_ConcurrentStressTest_HandlesHighLoad() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        var numberOfConcurrentRequests = 50;
        var tasks = new List<Task<mcCommandResponse>>();
        var errors = new ConcurrentBag<string>();

        // Act - Send many concurrent requests
        for (int i = 0; i < numberOfConcurrentRequests; i++) {
            var requestId = i;
            tasks.Add(Task.Run(async () => {
                try {
                    var request = new mcCommandRequest { Id = requestId, Content = $"CONCURRENT_TEST_{requestId}" };
                    return await client.SendCommandAsync(request);
                } catch (Exception ex) {
                    errors.Add($"Request {requestId}: {ex.Message}");
                    return new mcCommandResponse { Success = false, Message = ex.Message };
                }
            }));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Most requests should succeed under normal load
        var successfulResponses = responses.Count(r => r.Success);
        var successRate = (double)successfulResponses / numberOfConcurrentRequests;

        Assert.Multiple(() => {
            Assert.That(responses.Length, Is.EqualTo(numberOfConcurrentRequests));
            Assert.That(successRate, Is.GreaterThan(0.8), $"Success rate should be > 80%, got {successRate:P}");
            
            if (errors.Any()) {
                TestContext.WriteLine($"Errors encountered: {string.Join(", ", errors)}");
            }
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(29)]
    public async Task SendCommand_RapidSequentialRequests_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        var numberOfRequests = 100;
        var responses = new List<mcCommandResponse>();

        // Act - Send rapid sequential requests
        for (int i = 0; i < numberOfRequests; i++) {
            var request = new mcCommandRequest { Id = i, Content = $"RAPID_TEST_{i}" };
            var response = await client.SendCommandAsync(request);
            responses.Add(response);
        }

        // Assert - All requests should complete successfully
        Assert.Multiple(() => {
            Assert.That(responses.Count, Is.EqualTo(numberOfRequests));
            
            var successfulResponses = responses.Count(r => r.Success);
            Assert.That(successfulResponses, Is.EqualTo(numberOfRequests), 
                $"All {numberOfRequests} requests should succeed, but only {successfulResponses} did");

            // Verify responses are in correct order
            for (int i = 0; i < numberOfRequests; i++) {
                Assert.That(responses[i].Message, Is.EqualTo($"RAPID_TEST_{i}"), 
                    $"Response {i} should match request");
            }
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(30)]
    public void SendCommand_WithVeryLargeId_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with maximum int value
        var request = new mcCommandRequest { Id = int.MaxValue, Content = "MAX_ID_TEST" };
        var response = client.SendCommand(request);

        // Assert - Should handle large ID gracefully
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("MAX_ID_TEST"));
        });

        client.Disconnect();
    }

    [Test]
    [Order(31)]
    public async Task SendCommand_MultipleClientsRapidRequests_HandlesGracefully() {
        // Arrange - Create multiple clients
        var numberOfClients = 5;
        var requestsPerClient = 20;
        var clients = new List<GrpcClient>();
        var allTasks = new List<Task<mcCommandResponse>>();

        // Connect all clients
        for (int c = 0; c < numberOfClients; c++) {
            var commsHost = new CommsAddress(_host, _basePort);
            var client = new GrpcClient(commsHost);
            var connected = await client.ConnectAsync((_) => { });
            Assert.That(connected, Is.True, $"Client {c} should connect");
            clients.Add(client);
        }

        // Act - Each client sends multiple rapid requests
        for (int c = 0; c < numberOfClients; c++) {
            var clientIndex = c;
            var client = clients[clientIndex];
            
            for (int r = 0; r < requestsPerClient; r++) {
                var requestIndex = r;
                allTasks.Add(Task.Run(async () => {
                    var request = new mcCommandRequest { 
                        Id = clientIndex * 1000 + requestIndex, 
                        Content = $"CLIENT_{clientIndex}_REQ_{requestIndex}" 
                    };
                    return await client.SendCommandAsync(request);
                }));
            }
        }

        var allResponses = await Task.WhenAll(allTasks);

        // Assert - All responses should be successful
        var totalExpectedRequests = numberOfClients * requestsPerClient;
        var successfulResponses = allResponses.Count(r => r.Success);

        Assert.Multiple(() => {
            Assert.That(allResponses.Length, Is.EqualTo(totalExpectedRequests));
            Assert.That(successfulResponses, Is.GreaterThanOrEqualTo((int)(totalExpectedRequests * 0.95)), 
                $"At least 95% of {totalExpectedRequests} requests should succeed");
        });

        // Cleanup all clients
        foreach (var client in clients) {
            await client.DisconnectAsync();
        }
    }

    [Test]
    [Order(32)]
    public async Task SendCommand_WithUnicodeContent_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = await client.ConnectAsync((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with various Unicode characters
        var unicodeContent = "Unicode test: 中文 العربية עברית Русский हिन्दी 日本語 🎉🌟⭐";
        var request = new mcCommandRequest { Id = 1, Content = unicodeContent };
        var response = await client.SendCommandAsync(request);

        // Assert - Should handle Unicode characters
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo(unicodeContent));
        });

        await client.DisconnectAsync();
    }

    [Test]
    [Order(33)]
    public void SendCommand_WithBinaryLikeContent_HandlesGracefully() {
        // Arrange
        var commsHost = new CommsAddress(_host, _basePort);
        var client = new GrpcClient(commsHost);
        var connected = client.Connect((_) => { });
        Assert.That(connected, Is.True, "Client failed to connect to server");

        // Act - Send command with binary-like content (control characters)
        var binaryContent = "\0\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F";
        var request = new mcCommandRequest { Id = 1, Content = binaryContent };
        var response = client.SendCommand(request);

        // Assert - Should handle binary-like content
        Assert.Multiple(() => {
            Assert.That(response, Is.Not.Null);
            // Binary content might be handled differently by the server
            Assert.That(response.Success, Is.True);
        });

        client.Disconnect();
    }

    private static string GenerateRandomLongString() {
        var random = new Random();
        var length = 2048;
        // Length of the string to generate
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[length];
        for (int i = 0; i < length; i++) {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}
