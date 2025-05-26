# McComms.Sockets

Socket-based implementation for the McComms solution. This package provides TCP/IP socket-based implementations of the ICommsClient and ICommsServer interfaces from McComms.Core.

## TCP/IP Socket Communication Model

This implementation uses plain TCP/IP sockets with a simple text-based protocol for communication.

### Message Format

All messages are sent as UTF-8 encoded strings with the following format:

**Command Request:**
```
CMD:ID:MESSAGE
```

**Command Response:**
```
RES:SUCCESS:ID:MESSAGE
```

**Broadcast Message:**
```
BRD:ID:MESSAGE
```

### Connection Flow

```
+--------+                          +--------+
| Client |                          | Server |
+--------+                          +--------+
    |                                   |
    | TCP Connection Request            |
    |---------------------------------->|
    |                                   |
    | TCP Connection Established        |
    |<----------------------------------|
    |                                   |
```

### Command Flow

```
+--------+                          +--------+
| Client |                          | Server |
+--------+                          +--------+
    |                                   |
    | CMD:1:Hello Server                |
    |---------------------------------->|
    |                                   | Process
    |                                   | Command
    |                                   |
    | RES:true:1:Command processed      |
    |<----------------------------------|
    |                                   |
```

### Broadcast Flow

```
+--------+                          +--------+
| Server |                          | Client |
+--------+                          +--------+
    |                                   |
    | BRD:1:System notification         |
    |---------------------------------->|
    |                                   |
    |                           +--------+
    |                           | Client |
    |                           +--------+
    | BRD:1:System notification         |
    |---------------------------------->|
    |                                   |
```

## Example Usage

### Server

```csharp
// Create a Socket server on port 9000
var server = new CommsServerSockets(IPAddress.Parse("127.0.0.1"), 9000);

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
// Create a Socket client
var client = new CommsClientSockets(IPAddress.Parse("127.0.0.1"), 9000);

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

## Benefits of Socket-based Implementation

- **Lightweight**: Minimal overhead compared to higher-level protocols
- **Reliable**: Built on established TCP/IP technology
- **Cross-Platform**: Works on any platform that supports standard sockets
- **Direct Control**: Fine-grained control over connection handling
- **Low Latency**: Optimized for performance with minimal protocol overhead

## Contents
- Socket clients and servers
- Specific models and helpers

## Author
Joan Magnet
