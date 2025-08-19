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
- Use async/await for all network and I/O operations where possible.
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
### Client
```csharp
var client = new CommsClientSockets();
client.Connect(msg => Console.WriteLine($"Broadcast: {msg}"));
var response = await client.SendCommandAsync(new CommandRequest(1, "HELLO"));
client.Disconnect();
```

### Server
```csharp
var server = new CommsServerSockets();
server.RegisterCommandHandler("HELLO", req => new CommandResponse(true, req.Id.ToString(), "WORLD"));
server.Start();
server.Broadcast(new BroadcastMessage(1, "Welcome!"));
server.Stop();
```

## Contribution
- Fork, branch, and submit pull requests for new features or bug fixes.
- All code must pass CI/CD checks and include appropriate tests.

## License
MIT License. See LICENSE for details.

---
_These instructions are for GitHub Copilot and AI agents to provide context-aware, high-quality code suggestions for the McComms project._
