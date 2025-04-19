namespace McComms.Core;

// Represents a command request with an ID and a message.
public record CommandRequest(int Id = 0, string Message = "") {

    // Returns the string representation of the command request in the format "Id:Message".
    public override string ToString() => $"{Id}:{Message}";
}
