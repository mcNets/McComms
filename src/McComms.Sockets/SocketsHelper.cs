// Helper class for socket message framing and encoding/decoding
using System.Text;

namespace McComms.Sockets;

/// <summary>
/// Provides utilities for framing and encoding/decoding messages for socket communications.
/// </summary>
public class SocketsHelper
{
    // Control characters for message framing (ASCII)
    public const byte STX = 0x02; // Start of Text
    public const byte ETX = 0x03; // End of Text
    public const byte EOT = 0x04; // End of Transmission
    public const byte ACK = 0x06; // Acknowledge
    public const byte NAK = 0x15; // Negative Acknowledge
    public const byte GS = 0x1D;  // Group Separator

    // Predefined framed messages for protocol control
    public static readonly byte[] EOT_MSG = [STX, EOT, ETX];
    public static readonly byte[] ACK_MSG = [STX, ACK, ETX];
    public static readonly byte[] NAK_MSG = [STX, NAK, ETX];

    /// <summary>
    /// Encode a text message to UTF-8 bytes and wrap it with STX and ETX.
    /// </summary>
    /// <param name="message">Message to encode</param>
    /// <returns>Encoded bytes array</returns>
    public static byte[] Encode(string message) {
        // Uses UTF-8 to encode the text
        var encoded = Encoding.UTF8.GetBytes(message);
        return Framed(encoded);
    }
    
    /// <summary>
    /// Decode a bytes array to string (without removing framing).
    /// </summary>
    /// <param name="response">Received bytes array</param>
    /// <returns>Decoded message</returns>
    public static string Decode(byte[] response) {
        var msg = Encoding.UTF8.GetString(response, 0, response.Length);
        return msg;
    }

    /// <summary>
    /// Decode a ReadOnlySpan<byte> to string (without removing framing).
    /// </summary>
    /// <param name="response">Received bytes span</param>
    /// <returns>Decoded message</returns>
    public static string Decode(ReadOnlySpan<byte> response) {
        return Encoding.UTF8.GetString(response);
    }

    /// <summary>
    /// Encapsulates a byte array between STX and ETX.
    /// </summary>
    /// <param name="buffer">Message to encapsulate</param>
    /// <returns>Byte array with framing</returns>
    private static byte[] Framed(byte[] buffer) {
        // Optimized: uses Span<byte> for better efficiency (if needed, but optimization is minimal here)
        byte[] framed = new byte[buffer.Length + 2];
        framed[0] = STX;
        buffer.CopyTo(framed, 1);
        framed[^1] = ETX;
        return framed;
    }
}
