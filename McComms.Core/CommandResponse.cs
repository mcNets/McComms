namespace McComms.Core;

/// <summary>
/// Response returned by the server after a command is processed.
/// </summary>
public record CommandResponse(bool Success = false, string? Id = "", string? Message = "") {
    // Returns the string representation of the command response in the format "Success:Id:Message".
    public override string ToString() => $"{Success}:{Id}:{Message}";
}

