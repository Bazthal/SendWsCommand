using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


class Program
{
    /// <summary>
    /// Entry point for the application that sends a command to a WebSocket server.
    /// </summary>
    /// <remarks>This method establishes a WebSocket connection to the specified server, sends a JSON-encoded
    /// command, and then closes the connection. The JSON object includes a "Command" field and an optional "Value"
    /// field. If the connection or sending fails, an error message is printed to the console.</remarks>
    /// <param name="args">Command-line arguments where the first argument is the WebSocket server address and endpoint in the format
    /// <c>address:port/endpoint</c>, the second argument is the command to send, and an optional third argument is the
    /// value associated with the command.</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: SendWsCommand <address:port/endpoint> <Command> [Value]");
            return;
        }

        string url = $"ws://{args[0]}";
        string command = args[1];
        string? value = args.Length > 2 ? args[2] : null;

        var jsonObj = new
        {
            Command = command,
            Value = string.IsNullOrEmpty(value) ? null : value
        };

        string json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull

        });

        using var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

            Console.WriteLine($"Sent JSON: {json}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
