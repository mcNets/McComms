# McComms - Flexible Communications Library

McComms is a flexible communications library that provides client and server implementations using different communication technologies.

## Overview

McComms is designed to offer a common interface for client-server communications, allowing different underlying implementations. Currently, the following technologies are supported:

- **TCP/IP Sockets** - Socket-based communications implementation
- **gRPC** - gRPC-based communications implementation

The library is structured to allow easy substitution of the communication technology without changing the main application code.

## Project Structure

The project is organized into the following modules:

- **McComms.Core** - Defines common interfaces and base classes
- **McComms.Sockets** - TCP/IP sockets based implementation
- **McComms.gRPC** - gRPC based implementation
- **McComms.Core.Tests** - Unit tests for the library core

## Main Features

- Unified interface for different communication technologies
- Support for both synchronous and asynchronous operations
- Client-server communications management
- Support for broadcast messages
- Message encoding and decoding

## Installation

The project is available as NuGet packages:

```
Install-Package McComms.Core
Install-Package McComms.Sockets
Install-Package McComms.gRPC
```

## Basic Usage

### Client

```csharp
// Create a client using sockets
var client = new CommsClientSockets();

// Connect to the server
client.Connect(message => {
    Console.WriteLine($"Broadcast received: {message}");
});

// Send a command and get a response
var response = client.SendCommand(new CommandRequest("COMMAND", "parameters"));

// Or using async/await
var response = await client.SendCommandAsync(new CommandRequest("COMMAND", "parameters"));

// Disconnect
client.Disconnect();
```

### Server

```csharp
// Create a server using sockets
var server = new CommsServerSockets();

// Register command handlers
server.RegisterCommandHandler("COMMAND", (request) => {
    return new CommandResponse("OK");
});

// Start the server
server.Start();

// Send a broadcast message to all clients
server.Broadcast(new BroadcastMessage("INFO", "Message for all clients"));

// Stop the server
server.Stop();
```

## Support for Synchronous and Asynchronous Operations

All main operations support both synchronous and asynchronous versions, allowing you to choose the approach that best suits your requirements:

```csharp
// Synchronous version
var response = client.SendCommand(new CommandRequest("COMMAND", "data"));

// Asynchronous version
var response = await client.SendCommandAsync(new CommandRequest("COMMAND", "data"));
```

## Contribution

Contributions are welcome! If you wish to contribute to McComms, please:

1. Fork the repository
2. Create a branch for your feature (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## License

This project is licensed under the MIT License. See the LICENSE file for details.
