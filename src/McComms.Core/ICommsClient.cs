namespace McComms.Core;

/// <summary>
/// Interface for a communications client. Handles connection, command sending, and broadcast reception.
/// </summary>
public interface ICommsClient {
    /// <summary>
    /// Gets the host information for the communications client.
    /// </summary>
    NetworkAddress Address { get; }

    /// <summary>
    /// Connects to the server and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    bool Connect(Action<BroadcastMessage>? onBroadcastReceived);

    /// <summary>
    /// Connects to the server asynchronously and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<bool> ConnectAsync(Action<BroadcastMessage>? onBroadcastReceived, CancellationToken token = default);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Disconnects from the server asynchronously.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a command to the server and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <returns>The response from the server.</returns>
    CommandResponse SendCommand(CommandRequest msg);

    /// <summary>
    /// Sends a command to the server asynchronously and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <param name="token">Cancellation token to cancel the operation.</param>
    /// <returns>The response from the server.</returns>
    Task<CommandResponse> SendCommandAsync(CommandRequest msg, CancellationToken token = default);

    /// <summary>
    /// Gets or sets the callback for broadcast messages received from the server.
    /// </summary>
    Action<BroadcastMessage>? OnBroadcastReceived { get; set; }
}
