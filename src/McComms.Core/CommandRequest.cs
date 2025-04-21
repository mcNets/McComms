namespace McComms.Core;

public record CommandRequest(int Id = 0, string Message = "") {

    public override string ToString() => $"{Id}:{Message}";
}
