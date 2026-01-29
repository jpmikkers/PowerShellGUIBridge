using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GuiWorker.ViewModels;

public class NamedPipeClientService : BackgroundService
{
    private readonly string _pipeName;
    private readonly TimeSpan _connectionTimeout;

    public event Action<string>? StateChanged;
    public event Func<string, Task>? PostMessageReceived;
    public event Func<string, Task<string>>? InvokeMessageReceived;

    public NamedPipeClientService(string pipeName, TimeSpan connectionTimeout)
    {
        _pipeName = pipeName;
        _connectionTimeout = connectionTimeout;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OnStateChanged("Connecting");
        using var pipeClientStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipeClientStream.ConnectAsync(_connectionTimeout, stoppingToken);
        OnStateChanged("Connected");
        await ProcessMessagesAsync(pipeClientStream, stoppingToken);
    }

    // the .NET NamedPipeClientStream.ReadAtLeast does not function property in linux, so we implement our own version
    private async Task MyReadExactly(PipeStream pipe, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        while (count > 0)
        {
            var done = await pipe.ReadAsync(buffer, offset, count, cancellationToken);

            if (done == 0)
            {
                throw new EndOfStreamException("End of pipe stream reached while attempting to read data.");
            }
            else
            {
                offset += done;
                count -= done;
            }
        }
    }

    private async Task ProcessMessagesAsync(PipeStream pipe, CancellationToken cancellationToken)
    {
        var responseTag = new byte[] { (byte)'M', (byte)'S', (byte)'G', (byte)'R' };
        var header = new byte[8];
        var message = new byte[1024];

        ArgumentNullException.ThrowIfNull(pipe);

        while (!cancellationToken.IsCancellationRequested)
        {
            await MyReadExactly(pipe, header, 0, 8, cancellationToken);

            if (header[0] == 'M' && header[1] == 'S' && header[2] == 'G' && header[3] == 'P')
            {
                var messageLength = BitConverter.ToInt32(header, 4);

                if (message.Length < messageLength)
                {
                    message = new byte[messageLength];
                }

                await MyReadExactly(pipe, message, 0, messageLength, cancellationToken);
                var messageText = Encoding.UTF8.GetString(message.AsSpan(0, messageLength));
                
                if (PostMessageReceived != null)
                {
                    await PostMessageReceived(messageText);
                }
            }
            else if (header[0] == 'M' && header[1] == 'S' && header[2] == 'G' && header[3] == 'I')
            {
                var messageLength = BitConverter.ToInt32(header, 4);

                if (message.Length < messageLength)
                {
                    message = new byte[messageLength];
                }

                await MyReadExactly(pipe,message, 0, messageLength, cancellationToken);
                var messageText = Encoding.UTF8.GetString(message.AsSpan(0, messageLength));
                
                var response = InvokeMessageReceived != null 
                    ? await InvokeMessageReceived(messageText)
                    : "ok";

                var responseBytes = Encoding.UTF8.GetBytes(response);

                await pipe.WriteAsync(responseTag, 0, 4, cancellationToken);
                await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length), 0, 4, cancellationToken);
                await pipe.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Invalid message header");
            }
        }
    }

    private void OnStateChanged(string state)
    {
        StateChanged?.Invoke(state);
    }
}