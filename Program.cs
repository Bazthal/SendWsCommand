using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    /// <summary>
    /// Entry point for the application that sends a WebSocket command to a specified address.
    /// </summary>
    /// <remarks>This method establishes a WebSocket connection to the specified address and sends a JSON
    /// payload containing the command. The connection is closed after the message is sent. If the required parameters
    /// "Address" and "Command" are not provided, the method outputs usage instructions and terminates. Legacy argument
    /// format is supported but deprecated.</remarks>
    /// <param name="args">Command-line arguments specifying the WebSocket address and command.  The preferred format is "Address=host
    /// Command=cmd [Value=val] [OtherKey=otherVal]".</param>
    /// <returns></returns>
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Preferred: SendWsCommand Address=host Command=cmd [Value=val] [OtherKey=otherVal]");
            Console.WriteLine("  Legacy (deprecated): SendWsCommand host command [value] [order]");
            return;
        }

        var kvArgs = ParseArguments(args);

        if (!kvArgs.TryGetValue("Address", out var address) || !kvArgs.TryGetValue("Command", out var command))
        {
            Console.WriteLine("Missing required parameters: Address and Command");
            return;
        }

        var payload = new Dictionary<string, string?>
        {
            ["Command"] = command
        };

        if (kvArgs.TryGetValue("Value", out var value)) payload["Value"] = value;
        if (kvArgs.TryGetValue("Order", out var order)) payload["Order"] = order;

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(new Uri($"ws://{address}"), CancellationToken.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

            Console.WriteLine($"Sent JSON to {address}:\n{json}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("WebSocket Error: " + ex.Message);
        }
    }

    /// <summary>
    /// Parses an array of command-line arguments into a dictionary of key-value pairs.
    /// </summary>
    /// <remarks>The method supports two formats for arguments: key=value pairs and a legacy positional
    /// format.  The key=value format is preferred and allows for case-insensitive keys.  If the legacy format is used,
    /// a warning is displayed, and the arguments are mapped to specific keys.</remarks>
    /// <param name="args">An array of strings representing the command-line arguments. Arguments can be in the form of key=value pairs or
    /// positional arguments.</param>
    /// <returns>A dictionary containing the parsed arguments. If key=value pairs are provided, they are added to the dictionary.
    /// If positional arguments are used, they are mapped to predefined keys: "Address", "Command", "Value", and
    /// "Order".</returns>
    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Try to parse key=value style
        bool anyKeyValue = args.Any(arg => arg.Contains('='));
        if (anyKeyValue)
        {
            foreach (var arg in args)
            {
                var parts = arg.Split('=', 2);
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Invalid argument (expected key=value): {arg}");
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    Console.WriteLine($"Invalid argument (empty key): {arg}");
                    continue;
                }

                dict[key] = value;
            }
            return dict;
        }

        // Fallback: Legacy positional format
        if (args.Length >= 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Positional arguments are deprecated. Use key=value format instead.");
            Console.ResetColor();

            dict["Address"] = args[0];
            dict["Command"] = args[1];

            if (args.Length > 2) dict["Value"] = args[2];
            if (args.Length > 3) dict["Order"] = args[3];
        }
        else
        {
            Console.WriteLine("Error: Not enough arguments.");
        }

        return dict;
    }


}
