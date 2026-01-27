using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AvaloniaNamedPipe.ViewModels;

public class NamedPipeClient : BackgroundService
{
    private NamedPipeClientStream? _pipeClientStream;
    private readonly string _pipeName;
    private readonly TimeSpan _connectionTimeout;

    public event Action<string>? StateChanged;
    public event Func<string, Task>? PostMessageReceived;
    public event Func<string, Task<string>>? InvokeMessageReceived;

    public NamedPipeClient(string pipeName, TimeSpan connectionTimeout)
    {
        _pipeName = pipeName;
        _connectionTimeout = connectionTimeout;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OnStateChanged("Connecting");

        try
        {
            _pipeClientStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            // _pipeClientStream.ReadTimeout = Timeout.Infinite;
            await _pipeClientStream.ConnectAsync(_connectionTimeout, stoppingToken);
            //(int)TimeSpan.FromSeconds(60).TotalMilliseconds;
            //_pipeClientStream.WriteTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
        }
        catch
        {
            OnStateChanged("Failed to connect");
            return;
        }

        OnStateChanged("Connected");

        try
        {
            await ProcessMessagesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            OnStateChanged($"Error: {ex}");
        }
        finally
        {
            _pipeClientStream?.Dispose();
            //OnStateChanged("Disconnected");
        }
    }

    // the .NET NamedPipeClientStream.ReadAtLeast does not function property in linux, so we implement our own version
    private async Task MyReadExactly(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        while (count > 0)
        {
            int done = await _pipeClientStream!.ReadAsync(buffer, offset, count, cancellationToken);

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

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var message = new byte[1024];
        var throwOnEndOfStream = true;

        while (true)
        {
            await MyReadExactly(header, 0, 8, cancellationToken);

            if (header[0] == 'M' && header[1] == 'S' && header[2] == 'G' && header[3] == 'P')
            {
                var messageLength = BitConverter.ToInt32(header, 4);

                if (message.Length < messageLength)
                {
                    message = new byte[messageLength];
                }

                await MyReadExactly(message, 0, messageLength, cancellationToken);
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

                await MyReadExactly(message, 0, messageLength, cancellationToken);
                var messageText = Encoding.UTF8.GetString(message.AsSpan(0, messageLength));
                
                var response = InvokeMessageReceived != null 
                    ? await InvokeMessageReceived(messageText)
                    : "ok";

                var responseBytes = Encoding.UTF8.GetBytes(response);

                await _pipeClientStream.WriteAsync(new byte[] { (byte)'M', (byte)'S', (byte)'G', (byte)'R' }, 0, 4, cancellationToken);
                await _pipeClientStream.WriteAsync(BitConverter.GetBytes(responseBytes.Length), 0, 4, cancellationToken);
                await _pipeClientStream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Invalid message header (connected: {_pipeClientStream.IsConnected})");
            }
        }
    }

    private void OnStateChanged(string state)
    {
        StateChanged?.Invoke(state);
    }
}