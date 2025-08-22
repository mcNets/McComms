# Copilot Instructions for McComms

## Project Summary
McComms is a flexible .NET communications library providing unified client and server interfaces for multiple transport technologies:
- TCP/IP Sockets
- gRPC
- WebSockets

The library is modular, allowing easy substitution of the underlying communication protocol without changing application code. It supports both synchronous and asynchronous operations, command/response messaging, and server-initiated broadcast messages.

## Key Models
- `CommandRequest(int Id, string Message)` — Sent from client to server to request an operation.
- `CommandResponse(bool Success, string? Id, string? Message)` — Sent from server to client in response to a command.
- `BroadcastMessage(int Id, string Message)` — Sent from server to all clients for notifications.

## Coding Guidelines
- Use async/await for all network and I/O operations where possible but build both cases.
- Follow .NET naming conventions (PascalCase for public members, camelCase for locals/parameters).
- Document all public APIs with XML comments.
- Prefer using ArrayPool for buffer management in performance-sensitive code.
- Ensure thread safety for all server and client connection management.
- All message encoding/decoding should use the provided helper utilities.

## Custom Instructions for Copilot
- When generating new code, always use the existing models and interfaces from McComms.Core.
- For new features, provide both synchronous and asynchronous API variants if applicable.
- When adding new communication protocols, follow the structure of existing modules (see McComms.Sockets, McComms.gRPC, McComms.WebSockets).
- For tests, use NUnit and follow the structure of the existing test projects.
- When in doubt, prefer clarity and maintainability over micro-optimizations.

## Example Usage
### Server

```csharp
// Create a server using sockets
NetworkAddress networkAddress = new NetworkAddress("127.0.0.1", 5000);
ICommsServer server = new CommsServerSockets(networkAddress);

// Start the server and set the command handler.
server.Start(OnCommandReceived);

// Send a broadcast message to all clients
server.Broadcast(new BroadcastMessage(100, "Message for all clients"));

// Stop the server
server.Stop();

// Command handler
CommandResponse OnCommandReceived(CommandRequest request) {
    switch (request.Id) {
        case 1:
            // Process Command 1
            // Encode Broadcast messages to avoid conflicts.
            return new CommandResponse(true, request.Id.ToString(), "Command 1, OK");
            break;  
    }
}
```

### Client

```csharp
// Create a client using sockets
NetworkAddress networkAddress = new NetworkAddress("127.0.0.1", 5000);
ICommsClient client = new CommsClientSockets(networkAddress);

// Connect to the server
client.Connect(OnBroadcastReceived);

// Send a command and get a response
var command = "1,parameter1,parameter2";
if (command.TryParseCommandRequest(out commandRequest)) {
    var response = client.SendCommand(commandRequest);
    Console.WriteLine($"Success: {response.Success}, ID: {response.Id}, Message: {response.Message}");
}


---
_These instructions are for GitHub Copilot and AI agents to provide context-aware, high-quality code suggestions for the McComms project._
