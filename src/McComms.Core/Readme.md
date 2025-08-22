# McComms.Core

Base project with common interfaces, models and helpers for the McComms solution. This library provides the basic structure for communication between clients and servers using different technologies. Once implemented, it can be extended to support various transport mechanisms like Sockets, WebSockets, or gRPC.

### Communication Interfaces

#### ICommsServer
Interface that defines the operations of a communications server:
- **Address**: Gets the address of the server.
- **Start(Func<CommandRequest, CommandResponse>?, CancellationToken)**: Starts the server with a callback for received commands.
- **Stop()**: Stops the server.
- **StopAsync()**: Stops the server asynchronously.
- **SendBroadcast(BroadcastMessage)**: Sends a broadcast message to all connected clients.
- **SendBroadcastAsync(BroadcastMessage, CancellationToken)**: Sends a broadcast message to all connected clients asynchronously.

#### ICommsClient
Interface that defines the operations of a communications client:
- **Address**: Gets the address of the server.
- **Connect(Action<BroadcastMessage>?)**: Connects to the server and sets the callback for broadcast messages.
- **ConnectAsync(Action<BroadcastMessage>?, CancellationToken)**: Connects to the server asynchronously and sets the callback for broadcast messages.
- **Disconnect()**: Disconnects from the server.
- **DisconnectAsync()**: Disconnects from the server asynchronously.
- **SendCommand(CommandRequest)**: Sends a command to the server and returns the response.
- **SendCommandAsync(CommandRequest, CancellationToken)**: Sends a command to the server asynchronously and returns the response.
- **OnBroadcastReceived**: Property that sets the callback for received broadcast messages.

### Data Models

#### CommandRequest
Record that represents a command request:
- **Id**: Numeric identifier of the command.
- **Message**: Message associated with the command.
- **ToString()** method: Returns the representation in "Id:Message" format.

#### CommandResponse
Record that represents a response to a command:
- **Success**: Indicates whether the command was processed successfully.
- **Id**: Command identifier.
- **Message**: Response message.
- **ToString()** method: Returns the representation in "Success:Id:Message" format.

#### BroadcastMessage
Record that represents a broadcast message:
- **Id**: Numeric identifier of the message.
- **Message**: Content of the message.
- **ToString()** method: Returns the representation in "Id:Message" format.

### Helpers and Extensions

#### McCommsExtensions
Class with extension methods for processing communication formats:
- **TryParseCommandRequest(string)**: Attempts to parse a string in "Id:Message" format into a CommandRequest.
- **TryParseCommandResponse(string)**: Attempts to parse a string in "Success:Id:Message" format into a CommandResponse.
- **TryParseBroadcastMessage(string)**: Attempts to parse a string in "Id:Message" format into a BroadcastMessage.

## Implementations
This core library is implemented by other projects in the solution:
- McComms.gRPC: Implementation using gRPC.
- McComms.Sockets: Implementation using TCP sockets.
- McComms.WebSockets: Implementation using WebSockets.

## Author
Joan Magnet
