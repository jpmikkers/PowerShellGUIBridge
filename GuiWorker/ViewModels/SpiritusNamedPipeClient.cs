using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AvaloniaNamedPipe.ViewModels;

public class SpiritusMessage
{
    public string Command { get; set; } = "";
    public JsonElement Payload { get; set; }
}

public class SpiritusResponse
{
    public string Response { get; set; } = "";
    public object? Payload { get; set; }
}

public class SpiritusNamedPipeClient
{
    private readonly NamedPipeClient _namedPipeClient;

    public event Action<string>? StateChanged;
    public event Func<SpiritusMessage, Task>? PostMessageReceived;
    public event Func<SpiritusMessage, Task<SpiritusResponse>>? InvokeMessageReceived;

    public SpiritusNamedPipeClient(NamedPipeClient namedPipeClient)
    {
        _namedPipeClient = namedPipeClient;
        _namedPipeClient.StateChanged += OnStateChanged;
        _namedPipeClient.PostMessageReceived += OnPostMessageReceived;
        _namedPipeClient.InvokeMessageReceived += OnInvokeMessageReceived;
    }

    private void OnStateChanged(string state)
    {
        StateChanged?.Invoke(state);
    }

    private async Task OnPostMessageReceived(string messageJson)
    {
        try
        {
            var spiritusMessage = JsonSerializer.Deserialize<SpiritusMessage>(messageJson);
            if (spiritusMessage != null && PostMessageReceived != null)
            {
                await PostMessageReceived(spiritusMessage);
            }
        }
        catch
        {
            // Handle JSON deserialization error
        }
    }

    private async Task<string> OnInvokeMessageReceived(string messageJson)
    {
        try
        {
            var spiritusMessage = JsonSerializer.Deserialize<SpiritusMessage>(messageJson);
            if (spiritusMessage != null && InvokeMessageReceived != null)
            {
                var response = await InvokeMessageReceived(spiritusMessage);
                return JsonSerializer.Serialize(response);
            }
        }
        catch
        {
            // Handle JSON deserialization error
        }

        var errorResponse = new SpiritusResponse { Response = "error" };
        return JsonSerializer.Serialize(errorResponse);
    }
}