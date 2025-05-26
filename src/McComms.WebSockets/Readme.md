# McComms.WebSockets

WebSockets implementation for the McComms communication library.

This package provides WebSockets-based implementations of the ICommsClient and ICommsServer interfaces from McComms.Core, allowing for bidirectional communication over WebSockets.

## Communication Flow with WebSockets

WebSockets provide a full-duplex communication channel over a single TCP connection, making them ideal for real-time applications.

### Connection Establishment

```
+--------+                           +--------+
| Client |                           | Server |
+--------+                           +--------+
    |                                    |
    | WebSocket Connection Request       |
    |----------------------------------->|
    |                                    |
    | WebSocket Connection Established   |
    |<-----------------------------------|
    |                                    |
```

### Command Request/Response

```
+--------+                           +--------+
| Client |                           | Server |
+--------+                           +--------+
    |                                    |
    | WS Message: CommandRequest JSON    |
    |----------------------------------->|
    |                                    | Process
    |                                    | Command
    |                                    |
    | WS Message: CommandResponse JSON   |
    |<-----------------------------------|
    |                                    |
```

### Broadcast Message

```
+--------+                           +--------+
| Server |                           | Client |
+--------+                           +--------+
    |                                    |
    | WS Message: BroadcastMessage JSON  |
    |----------------------------------->|
    |                                    |
    |                            +--------+
    |                            | Client |
    |                            +--------+
    | WS Message: BroadcastMessage JSON  |
    |----------------------------------->|
    |                                    |
```

## Example

### Server

```csharp
// Create a WebSockets server on port 8080
var server = new CommsServerWebSockets("localhost", 8080);

// Start the server with a command handler
server.Start((request) => {
    Console.WriteLine($"Received command: {request.Id} - {request.Message}");
    return new CommandResponse(true, request.Id.ToString(), "Command processed");
});

// Send a broadcast message to all connected clients
server.SendBroadcast(new BroadcastMessage(1, "System notification"));

// Later, when application ends
server.Stop();
```

### Client

```csharp
// Create a WebSockets client
var client = new CommsClientWebSockets("localhost", 8080);

// Connect and set up broadcast message handler
var connected = await client.ConnectAsync((broadcast) => {
    Console.WriteLine($"Received broadcast: {broadcast.Id} - {broadcast.Message}");
});

if (connected)
{
    // Send a command
    var request = new CommandRequest(1, "Hello Server");
    var response = await client.SendCommandAsync(request);
    
    Console.WriteLine($"Response: {response.Success} - {response.Message}");
    
    // When done
    await client.DisconnectAsync();
}
```

## Features

- WebSockets client implementation
- WebSockets server implementation
- Compatible with all McComms core interfaces
- Real-time bidirectional communication
- Asynchronous APIs for modern application development

## Usage

Refer to the demo applications for more examples of how to use this library.
