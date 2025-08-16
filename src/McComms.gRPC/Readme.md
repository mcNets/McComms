# McComms.gRPC

gRPC implementation for the McComms solution. This package provides gRPC-based implementations of the ICommsClient and ICommsServer interfaces from McComms.Core.

>NOTE:
>
>**gRPC over HTTP/2 is not natively supported by most browsers due to limitations in browser APIs and the lack of full HTTP/2 support for custom protocols. As a result, this package cannot be used directly from web applications running in browsers, and it does not support Cross-Origin Resource Sharing (CORS). For browser-based scenarios, consider using gRPC-Web or a REST API instead, or the WebSockets implementation in McComms.WebSockets.**

## gRPC Communication Model

gRPC uses Protocol Buffers (protobuf) for efficient serialization and HTTP/2 for transport.

### Protocol Structure

The gRPC service is defined in the `commands.proto` file:

```protobuf
syntax = "proto3";

package commsproto;

service CommandService {
  rpc SendCommand (mcCommandRequest) returns (mcCommandResponse);
  rpc SubscribeToBroadcast (Empty) returns (stream mcBroadcast);
}

message mcCommandRequest {
  int32 id = 1;
  string content = 2;
}

message mcCommandResponse {
  bool success = 1;
  string message = 2;
}

message mcBroadcast {
  int32 id = 1;
  string content = 2;
}

message Empty {}

```

### Command Flow

```
+--------+                        +--------+
| Client |                        | Server |
+--------+                        +--------+
    |                                 |
    | gRPC: SendCommand(mcCommandRequest)
    |-------------------------------->|
    |                                 | Process
    |                                 | Command
    |                                 |
    | mcCommandResponse               |
    |<--------------------------------|
    |                                 |
```

### Broadcast Flow (Stream-Based)

```
+--------+                        +--------+
| Client |                        | Server |
+--------+                        +--------+
    |                                 |
    | SubscribeToBroadcast(Empty)     |
    |-------------------------------->|
    |                                 |
    | Stream Established              |
    |<--------------------------------|
    |                                 |
    | mcBroadcast Message 1           |
    |<--------------------------------|
    |                                 |
    | mcBroadcast Message 2           |
    |<--------------------------------|
    |          ...                    |
```

## Example Usage

### Server

```csharp
// Create a gRPC server on port 50051
var server = new CommsServerGrpc("localhost", 50051);

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
// Create a gRPC client
var client = new CommsClientGrpc("localhost", 50051);

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

## Benefits of gRPC

- **High Performance**: Uses HTTP/2 for multiplexing and Protocol Buffers for efficient serialization
- **Strong Typing**: Contract-first approach with .proto files
- **Bi-directional Streaming**: Perfect for real-time updates via broadcast
- **Language Agnostic**: Clients and servers can be implemented in different languages
- **Generated Code**: Reduces boilerplate and ensures type safety

## Contents
- gRPC services
- Clients and servers
- .proto files

## Author
Joan Magnet
