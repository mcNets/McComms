# McComms - Flexible Communications Library

McComms is a flexible communications library that provides client and server implementations using different communication technologies.

## Overview

McComms is designed to offer a common interface for client-server communications, allowing different underlying implementations. Currently, the following technologies are supported:

- **TCP/IP Sockets** - Socket-based communications implementation
- **gRPC** - gRPC-based communications implementation
- **WebSockets** - WebSockets-based communications implementation

The library is structured to allow easy substitution of the communication technology without changing the main application code.

## Project Structure

The project is organized into the following modules:

- **McComms.Core** - Defines common interfaces and base classes
- **McComms.Sockets** - TCP/IP sockets based implementation
- **McComms.gRPC** - gRPC based implementation
- **McComms.WebSockets** - WebSockets based implementation

## CI/CD and Package Management

This project uses GitHub Actions for continuous integration and delivery. The workflow includes:

- **Automatic build and test** on each push to main/master and on merged pull requests
- **Automatic versioning** using the format YYYY.MM.DD.X where X is the number of commits for the day
- **NuGet package generation** for all library components
- **GitHub Packages publishing** for easy distribution
- **Automatic GitHub Release creation** with release notes
- **McComms.Core.Tests** - Unit tests for the library core

## Main Features

- Unified interface for different communication technologies
- Support for both synchronous and asynchronous operations
- Client-server communications management
- Support for broadcast messages
- Message encoding and decoding

## Installation

The project is available as NuGet packages from GitHub Packages:

```
dotnet add package McComms.Core --version <VERSION>
dotnet add package McComms.Sockets --version <VERSION>
dotnet add package McComms.gRPC --version <VERSION>
dotnet add package McComms.WebSockets --version <VERSION>
```

Or through Package Manager:

```
Install-Package McComms.Core
Install-Package McComms.Sockets
Install-Package McComms.gRPC
Install-Package McComms.WebSockets
```

## Using NuGet Packages from GitHub Packages

To use the NuGet packages from GitHub Packages, you'll need to:

1. Create a GitHub Personal Access Token with `read:packages` scope
2. Add GitHub Packages as a source in your NuGet.config:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/OWNER/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Replace `OWNER` with the repository owner, `USERNAME` with your GitHub username, and `TOKEN` with your Personal Access Token.

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

## CI/CD Workflow Details

### Automatic Package Versioning

The GitHub Actions workflow automatically versions packages using a date-based scheme:

- Format: `YYYY.MM.DD.X` 
- Where `X` is the number of commits made during that day

This means that every time you push to the repository, the version number will automatically increment based on the current date and the number of commits made that day.

### NuGet Package Generation

The workflow automatically:

1. Builds and tests the solution
2. Calculates the appropriate version number
3. Generates NuGet packages for all library components
4. Uploads the packages as GitHub Actions artifacts
5. Copies them to the `src/Packages` directory in the repository
6. Publishes them to GitHub Packages (when pushing to main/master)
7. Creates a GitHub Release with the generated packages

### Workflow Triggers

The workflow runs on:
- Pushes to the main/master branch that affect files in the `src` directory or the workflow itself
- Merged pull requests to main/master that affect files in the `src` directory or the workflow itself

### Customizing the Workflow

You can customize the workflow by editing the `.github/workflows/nuget-build.yml` file. Some common customizations include:

- Changing version numbering scheme
- Publishing to additional NuGet feeds
- Adding additional build or test steps

## License

This project is licensed under the MIT License. See the LICENSE file for details.
