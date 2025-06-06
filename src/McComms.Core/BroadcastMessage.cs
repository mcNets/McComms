﻿namespace McComms.Core;

public record BroadcastMessage(int Id = 0, string Message = "") {
    public override string ToString() => $"{Id}:{Message}";
}
