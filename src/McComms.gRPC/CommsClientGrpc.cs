// filepath: c:\Code\McComms\src\McComms.gRPC\CommsClientGrpc.cs
using Commsproto;
using McComms.gRPC;

namespace McComms.gRPC;

/// <summary>
/// Implementation of ICommsClient based on gRPC.
/// This class adapts the functionality of GrpcClient to the standard ICommsClient interface.
/// </summary>
public class CommsClientGrpc : ICommsClient
{
    // Internal gRPC client that handles communications
    private readonly GrpcClient _client;

    /// <summary>
    /// Default constructor that initializes the client with default settings
    /// </summary>
    public CommsClientGrpc() {
        _client = new GrpcClient();       
    }

    /// <summary>
    /// Constructor that allows specifying host and port for the connection
    /// </summary>
    /// <param name="host">gRPC server address</param>
    /// <param name="port">gRPC server port</param>
    public CommsClientGrpc(string host, int port) {
        _client = new GrpcClient(host, port);
    }

    /// <summary>
    /// Callback that is invoked when a broadcast message is received
    /// </summary>
    public Action<BroadcastMessage>? OnBroadcastReceived { get; set; }

    /// <summary>
    /// Connects to the gRPC server and sets up broadcast listening
    /// </summary>
    /// <param name="onBroadcastReceived">Callback function that will be executed when a broadcast is received</param>
    /// <returns>true if the connection is started successfully</returns>
    public bool Connect(Action<BroadcastMessage>? onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        return _client.Connect(BroadcastReceived);
    }

    /// <summary>
    /// Internal method that adapts broadcast messages from gRPC format to standard format
    /// </summary>
    /// <param name="message">The broadcast message in gRPC format</param>
    private void BroadcastReceived(mcBroadcast message) {
        OnBroadcastReceived?.Invoke(new BroadcastMessage(message.Id, message.Content));
    }

    /// <summary>
    /// Safely disconnects from the gRPC server
    /// </summary>
    public void Disconnect() {
        _client.Disconnect();
    }

    /// <summary>
    /// Sends a command to the server and waits for its response
    /// </summary>
    /// <param name="msg">The command to send</param>
    /// <returns>The server's response</returns>
    public CommandResponse SendCommand(CommandRequest msg) {
        var response = _client.SendCommand(new mcCommandRequest { Id = msg.Id, Content = msg.Message });
        return new CommandResponse(response.Succes, response.Id, response.Message);
    }

    /// <summary>
    /// Sends an exit command to the server and disconnects
    /// </summary>
    public void SendExitCommand() {
        Disconnect();
    }
}