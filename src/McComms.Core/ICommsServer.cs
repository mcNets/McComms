﻿namespace McComms.Core;

/// <summary>
/// Interface for a communications server. Handles starting, stopping, broadcasting, and command reception.
/// </summary>
public interface  ICommsServer {
    /// <summary>
    /// Starts the server and sets the callback for received commands.
    /// </summary>
    /// <param name="onCommandReceived">Callback invoked when a command is received from a client.</param>
    /// <param name="stoppingToken">Token to signal server shutdown.</param>
    void Start(Func<CommandRequest, CommandResponse>? onCommandReceived, CancellationToken stoppingToken);

    /// <summary>
    /// Stops the server.
    /// </summary>
    void Stop();

    /// <summary>
    /// Sends a broadcast message to all connected clients.
    /// </summary>
    /// <param name="msg">The broadcast message to send.</param>
    void SendBroadcast(BroadcastMessage msg);

    /// <summary>
    /// Gets or sets the callback for when a command is received from a client.
    /// </summary>
    Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }
}