using System.Diagnostics;
using System.Threading;

namespace McComms.Sockets;

public class SocketsServer
{
    // Use SemaphoreSlim for async-friendly synchronization
    private readonly SemaphoreSlim _clientsSemaphore = new(1, 1);
    // TCP listener socket
    private readonly Socket _tcpListener;
    // Endpoint for the TCP listener
    private readonly IPEndPoint? _tcpEndPoint = null;
    // Last accepted client socket
    private Socket? _client;
    
    // List of connected clients
    private readonly List<SocketsClientModel> _clients = [];
    
    // Callback for handling received messages
    private Func<byte[], byte[]>? _onMessageReceived;
    
    // Buffer size constants
    private const int MAX_BUFFER_SIZE = 1500;
    private const int DEFAULT_PORT = 8888;

    // Default constructor: listens on any IP and default port
    public SocketsServer() {
        _tcpEndPoint = new IPEndPoint(IPAddress.Any, DEFAULT_PORT);
        _tcpListener = new Socket(_tcpEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    // Constructor with custom IP and port
    public SocketsServer(IPAddress ipAddress, int port) {
        _tcpEndPoint = new IPEndPoint(ipAddress, port);
        _tcpListener = new Socket(_tcpEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    // Starts listening for incoming client connections
    public void Listen(Func<byte[], byte[]> onMessageReceived, CancellationToken stoppingToken) {
        _onMessageReceived = onMessageReceived;
        _tcpListener.Bind(_tcpEndPoint!);
        _tcpListener.Listen();

        // Main accept loop
        while (stoppingToken.IsCancellationRequested == false) {
            _client = _tcpListener.Accept();
            var socketClient = new SocketsClientModel(_client, new NetworkStream(_client));

            // Use async-friendly semaphore instead of lock
            _clientsSemaphore.Wait();
            try {
                socketClient.Id = _clients.Count == 0 ? 1 : _clients.Max(x => x.Id) + 1;
                _clients.Add(socketClient);
            } finally {
                _clientsSemaphore.Release();
            }

            Debug.WriteLine($"Client connected, total clients: {_clients.Count}");

            // Handle client messages in a background task (async)
            _ = Task.Run(async () => await MessagesHandler(socketClient, stoppingToken), stoppingToken);
        }
    }

    // Broadcasts a command to all connected clients
    public void SendBroadcast(byte[] command) {
        _clientsSemaphore.Wait();
        try {
            foreach (var client in _clients.ToList()) {
                try {
                    if (client.Connected) {
                        client.Stream.Write(command);
                    }
                }
                catch {
                    // Remove disconnected clients
                    client.Connected = false;
                    _clients.Remove(client);
                    Debug.WriteLine($"Client disconnected, total: {_clients.Count}");
                }
            }
        } finally {
            _clientsSemaphore.Release();
        }
    }

    public async Task SendBroadcastAsync(byte[] command) {
        await _clientsSemaphore.WaitAsync();
        try {
            foreach (var client in _clients.ToList()) {
                try {
                    if (client.Connected) {
                        await client.Stream.WriteAsync(command, 0, command.Length);
                    }
                }
                catch {
                    // Assume client is disconnected and remove it
                    // This is a simplified error handling: in a real-world scenario, you might want to log the error or handle it differently
                    client.Connected = false;
                    _clients.Remove(client);
                    Debug.WriteLine($"Client disconnected, total: {_clients.Count}");
                }
            }
        } finally {
            _clientsSemaphore.Release();
        }
    }

    private async Task MessagesHandler(SocketsClientModel client, CancellationToken cancellationToken) {
        Debug.Assert(client != null);
        // Use a dynamic buffer for message processing, with initial capacity
        List<byte> bufferMessage = new(MAX_BUFFER_SIZE);
        byte[] buffer = new byte[MAX_BUFFER_SIZE];
        bool receivingMessage = false;
        try {
            // Main message processing loop
            while (!cancellationToken.IsCancellationRequested && client.Connected) {
                if (client.Stream.DataAvailable) {
                    var bytesRead = await client.Stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead <= 0) {
                        continue;
                    }
                    for (var x = 0; x < bytesRead; x++) {
                        switch (buffer[x]) {
                            case SocketsHelper.EOT:
                                // Handle end of transmission: send ACK and disconnect
                                byte[] ack = SocketsHelper.Encode("0:0");
                                await client.Stream.WriteAsync(ack, cancellationToken);
                                client.Connected = false;
                                break;
                            case SocketsHelper.STX:
                                // Start of new message
                                bufferMessage.Clear();
                                receivingMessage = true;
                                break;
                            case SocketsHelper.ETX:
                                // End of message: process and respond
                                if (receivingMessage && bufferMessage.Count > 0 && _onMessageReceived != null) {
                                    var message = bufferMessage.ToArray();
                                    var response = _onMessageReceived?.Invoke(message);
                                    if (response != null) {
                                        await client.Stream.WriteAsync(response, cancellationToken);
                                    }
                                }
                                receivingMessage = false;
                                bufferMessage.Clear();
                                break;
                            default:
                                // Buffer message content if inside a message
                                if (receivingMessage) {
                                    bufferMessage.Add(buffer[x]);
                                }
                                break;
                        }
                    }
                } else {
                    // Avoid busy waiting
                    await Task.Delay(5, cancellationToken);
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"McComms.Socket ERROR: {ex.Message}");
            throw;
        }
        finally {
            // Use async-friendly semaphore for client removal
            await _clientsSemaphore.WaitAsync();
            try {
                _clients.Remove(client);
            } finally {
                _clientsSemaphore.Release();
            }
            Debug.WriteLine($"Client disconnected, total: {_clients.Count}");
        }
    }
}

