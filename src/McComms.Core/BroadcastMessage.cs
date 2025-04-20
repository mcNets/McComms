namespace McComms.Core;

// Represents a broadcast message with an ID and a message.
public record BroadcastMessage(int Id = 0, string Message = "") {
    // Returns the string representation of the broadcast message in the format "Id:Message".
    public override string ToString() => $"{Id}:{Message}";
}
