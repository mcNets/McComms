namespace McComms.Core;

// Extension methods
public static class McCommsExtensions {

    // Attempts to parse a string into a CommandRequest record.
    // The expected format is "Id:Message".
    // Returns true if parsing is successful.
    public static bool TryParseCommandRequest(this string? request, out CommandRequest? commandRequest) {
        commandRequest = null;

        if (string.IsNullOrEmpty(request)) {
            return false;
        }

        var parts = request.Split(':');

        if (!int.TryParse(parts[0], out var id)) {
            return false;
        }

        var message = parts.Length > 1 ? string.Join(':', parts[1..]) : string.Empty;
        commandRequest = new CommandRequest(id, message);

        return true;
    }

    // Attempts to parse a string into a CommandResponse record.
    // The expected format is "Success:Id:Message".
    // Returns true if parsing is successful.
    public static bool TryParseCommandResponse(this string? msg, out CommandResponse? commandResponse) {
        commandResponse = null;

        if (string.IsNullOrEmpty(msg)) {
            return false;
        }

        var parts = msg.Split(':');

        if (parts.Length < 3) {
            return false;
        }

        var success = bool.TryParse(parts[0], out bool successValue) && successValue;
        var id = parts[1];
        var message = parts.Length > 2 ? string.Join(':', parts[2..]) : string.Empty;
        commandResponse = new CommandResponse(success, id, message);

        return true;
    }

    // Attempts to parse a string into a BroadcastMessage record.
    // The expected format is "Id:Message". 
    // Returns true if parsing is successful.
    public static bool TryParseBroadcastMessage(this string? request, out BroadcastMessage? broadcastMessage) {
        broadcastMessage = null;

        if (string.IsNullOrEmpty(request)) {
            return false;
        }

        var parts = request.Split(':');

        if (!int.TryParse(parts[0], out var id)) {
            return false;
        }

        var message = parts.Length > 1 ? string.Join(':', parts[1..]) : string.Empty;
        broadcastMessage = new BroadcastMessage(id, message);

        return true;
    }
}
