namespace McComms.Core;

/// <summary>
/// Interface for a communications client. Handles connection, command sending, and broadcast reception.
/// </summary>
public interface ICommsClient {
    /// <summary>
    /// Connects to the server and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    bool Connect(Action<BroadcastMessage> onBroadcastReceived);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Sends a command to the server and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <returns>The response from the server.</returns>
    CommandResponse SendCommand(CommandRequest msg);

    /// <summary>
    /// Sends an exit command to the server.
    /// </summary>
    void SendExitCommand();

    /// <summary>
    /// Gets or sets the callback for broadcast messages received from the server.
    /// </summary>
    Action<BroadcastMessage>? OnBroadcastReceived { get; set; }
}
