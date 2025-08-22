# McComms - Flexible Communications Library

McComms is a flexible communications library that provides client and server implementations using different communication technologies.

## Overview

McComms is designed to offer a common interface for client-server communications, allowing different underlying implementations. Currently, the following technologies are supported:

- **TCP/IP Sockets** - Socket-based communications implementation.
- **gRPC** - gRPC-based communications implementation.
- **WebSockets** - WebSockets-based communications implementation.

The library is structured to allow easy substitution of the communication technology without changing the main application code.

## Communication Models

### Command Request/Response Flow

```
+--------+                      +--------+
| Client |                      | Server |
+--------+                      +--------+
    |                               |
    | CommandRequest(Id, Message)   |
    |------------------------------>|
    |                               | Process
                                    | Command
                                    |
    | CommandResponse(Success, Id, Message)
    |<------------------------------|
    |                               |
```

**CommandRequest Model:**
```csharp
public record CommandRequest(int Id = 0, string Message = "");
```

| Parameter | Type   | Description                             |
|-----------|--------|-----------------------------------------|
| Id        | int    | Unique identifier for the command       |
| Message   | string | Command message content or parameters   |

**CommandResponse Model:**
```csharp
public record CommandResponse(bool Success = false, string? Id = "", string? Message = "");
```

| Parameter | Type   | Description                               |
|-----------|--------|-------------------------------------------|
| Success   | bool   | Indicates if the command was successful   |
| Id        | string | Identifies which command this responds to |
| Message   | string | Response content or result data           |

### Broadcast Message Flow

```
+--------+                      +--------+
| Server |                      | Client |
+--------+                      +--------+
    |                               |
    | BroadcastMessage(Id, Message) |
    |------------------------------>|
    |                               |
```

**BroadcastMessage Model:**
```csharp
public record BroadcastMessage(int Id = 0, string Message = "");
```

| Parameter | Type   | Description                              |
|-----------|--------|------------------------------------------|
| Id        | int    | Unique identifier for the broadcast type |
| Message   | string | Broadcast message content                |

## Project Structure

The project is organized into the following modules:

- **McComms.Core** - Defines common interfaces and base classes
- **McComms.Sockets** - TCP/IP sockets based implementation
- **McComms.gRPC** - gRPC based implementation
- **McComms.WebSockets** - WebSockets based implementation

## CI/CD and Package Management

This project uses GitHub Actions for continuous integration and delivery. The workflow includes:

- **Automatic build and test** on each push to main/master and on merged pull requests
- **NuGet package generation** for all library components
- **GitHub Packages publishing** for easy distribution
- **McComms.Core.Tests** - Unit tests for the library core

## Main Features

- Unified interface for different communication technologies
- Support for both synchronous and asynchronous operations
- Client-server communications management
- Support for broadcast messages
- Message encoding and decoding

## Installation

The project is available as NuGet packages from GitHub Packages:

[![NuGet](https://img.shields.io/nuget/v/McComms.Core.svg?label=McComms.Core)](https://www.nuget.org/packages/McComms.Core)
[![NuGet](https://img.shields.io/nuget/v/McComms.Sockets.svg?label=McComms.Sockets)](https://www.nuget.org/packages/McComms.Sockets)
[![NuGet](https://img.shields.io/nuget/v/McComms.gRPC.svg?label=McComms.gRPC)](https://www.nuget.org/packages/McComms.gRPC)
[![NuGet](https://img.shields.io/nuget/v/McComms.WebSockets.svg?label=McComms.WebSockets)](https://www.nuget.org/packages/McComms.WebSockets)

```
dotnet add package McComms.Sockets --version <VERSION>
dotnet add package McComms.gRPC --version <VERSION>
dotnet add package McComms.WebSockets --version <VERSION>
```

Or through Package Manager:

```
Install-Package McComms.Sockets
Install-Package McComms.gRPC
Install-Package McComms.WebSockets
```

## Basic Usage

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

// Disconnect
client.Disconnect();

// Broadcast message handler
void OnBroadcastReceived(BroadcastMessage message) {
    switch (message.Id) {
        case 100:
            // Process broadcast.
            Console.WriteLine($"Broadcast: {message.Id}, Message: {message.Message}");
            break;
    }
}
```


## Support for Synchronous and Asynchronous Operations

All main operations support both synchronous and asynchronous versions, allowing you to choose the approach that best suits your requirements:

```csharp
// Synchronous version
var response = client.SendCommand(new CommandRequest(100, "data"));

// Asynchronous version
var response = await client.SendCommandAsync(new CommandRequest(100, "data"), cancellationToken);
```

## Contribution

Contributions are welcome! If you wish to contribute to McComms, please:

1. Fork the repository
2. Create a branch for your feature (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## CI/CD Workflow Details

### NuGet Package Generation

The workflow automatically:

1. Builds and tests the solution
2. Generates NuGet packages for all library components
3. Uploads and publishes the packages as nuget.org artifacts

### Workflow Triggers

The workflow runs on:
- Pushes to the main/master branch that affect files in the `src` directory or the workflow itself
- Merged pull requests to main/master that affect files in the `src` directory or the workflow itself

## License

This project is licensed under the MIT License. See the LICENSE file for details.


