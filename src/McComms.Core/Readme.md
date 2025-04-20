# McComms.Core

Base project with common interfaces, models and helpers for the McComms solution. This library provides the basic structure for communication between clients and servers using different technologies.

## Contents

### Communication Interfaces

#### ICommsClient
Interface that defines the operations of a communications client:
- **Connect(Action<BroadcastMessage>)**: Connects to the server and sets the callback for broadcast messages.
- **Disconnect()**: Disconnects from the server.
- **SendCommand(CommandRequest)**: Sends a command to the server and returns the response.
- **SendExitCommand()**: Sends an exit command to the server.
- **OnBroadcastReceived**: Property that sets the callback for received broadcast messages.

#### ICommsServer
Interface that defines the operations of a communications server:
- **Start(Func<CommandRequest, CommandResponse>, CancellationToken)**: Starts the server with a callback for received commands.
- **Stop()**: Stops the server.
- **SendBroadcast(BroadcastMessage)**: Sends a broadcast message to all connected clients.
- **CommandReceived**: Property that sets the callback for received commands.

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

#### MsgHelper
Class with methods for creating command responses:
- **Ok()**: Creates a successful response without a message.
- **Ok(string)**: Creates a successful response with a message.
- **Fail()**: Creates a failed response without a message.
- **Fail(string, string)**: Creates a failed response with an ID and a message.

## Implementations
This core library is implemented by other projects in the solution:
- McComms.gRPC: Implementation using gRPC.
- McComms.Sockets: Implementation using TCP sockets.

## Author
Joan Magnet
