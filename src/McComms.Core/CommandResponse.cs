namespace McComms.Core;

public record CommandResponse(bool Success = false, string? Id = "", string? Message = "") {
    public override string ToString() => $"{Success}:{Id}:{Message}";
}

