using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiWorker.Views;

namespace GuiWorker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pipeState = "idle";

    [ObservableProperty]
    private string _info = "idle";

    public string[]? Args { get; set; }

    private NamedPipeClientService? _pipeClient;
    private SpiritusNamedPipeClient? _spiritusClient;
    private MainWindow _mainWindow = default!;
    private readonly ConcurrentQueue<SpiritusMessage> _messageQueue = new();

    public MainWindowViewModel()
    {
    }

    public async Task InitializeNamedPipe(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        if (Args is not null && Args.Length > 0)
        {
            _pipeClient = new NamedPipeClientService(Args[0], TimeSpan.FromSeconds(10.0));
            _spiritusClient = new SpiritusNamedPipeClient(_pipeClient);

            _spiritusClient.StateChanged += OnConnectionStateChanged;
            _spiritusClient.PostMessageReceived += OnPostMessageReceived;
            _spiritusClient.InvokeMessageReceived += OnInvokeMessageReceived;

            await _pipeClient.StartAsync(CancellationToken.None);

            _ = _pipeClient.ExecuteTask?.ContinueWith(
                x => {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _mainWindow?.Close();
                    });
                },
                TaskContinuationOptions.RunContinuationsAsynchronously | TaskContinuationOptions.NotOnRanToCompletion)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private async Task<object?> DoDrawCommand(SpiritusMessage message)
    {
        object? result = null;

        switch (message.Command)
        {
            case "Clear":
                _mainWindow.MyCanvas.Children.Clear();
                break;

            case "Sync":
                break;

            case "ShowOpenFileDialog":
                {
                    var openFileParams = message.Payload.Deserialize<SPOpenFileDialog>();
                    var topLevel = TopLevel.GetTopLevel(_mainWindow);
                    if (topLevel != null)
                    {
                        var options = new FilePickerOpenOptions
                        {
                            Title = openFileParams?.Title ?? "Open File",
                            AllowMultiple = openFileParams?.AllowMultiple ?? false,
                        };

                        if (openFileParams?.Filters != null && openFileParams.Filters.Count > 0)
                        {
                            var filters = new List<FilePickerFileType>();
                            foreach (var filter in openFileParams.Filters)
                            {
                                filters.Add(new FilePickerFileType(filter.Name ?? "All Files")
                                {
                                    Patterns = filter.Extensions ?? new List<string>()
                                });
                            }
                            options.FileTypeFilter = filters;
                        }

                        if (!string.IsNullOrEmpty(openFileParams?.SuggestedStartLocation))
                        {
                            options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(openFileParams.SuggestedStartLocation));
                        }

                        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
                        result = files.Select(x => x.TryGetLocalPath()).Where(x => x is not null).ToList();
                    }
                }
                break;

            case "ShowSaveFileDialog":
                {
                    var saveFileParams = message.Payload.Deserialize<SPSaveFileDialog>();
                    var topLevel = TopLevel.GetTopLevel(_mainWindow);
                    if (topLevel != null)
                    {
                        var options = new FilePickerSaveOptions
                        {
                            Title = saveFileParams?.Title ?? "Open File",
                            DefaultExtension = saveFileParams?.DefaultExtension,
                            SuggestedFileName = saveFileParams?.SuggestedFileName,
                            ShowOverwritePrompt = saveFileParams?.ShowOverwritePrompt ?? false
                        };

                        //if (saveFileParams.SuggestedFileType is not null)
                        //{
                        //    options.SuggestedFileType = new FilePickerFileType(saveFileParams.SuggestedFileType)
                        //    {
                        //        Patterns = new List<string>() { $"*.{saveFileParams.SuggestedFileType}" }
                        //    };
                        //}

                        if (saveFileParams?.Filters != null && saveFileParams.Filters.Count > 0)
                        {
                            var filters = new List<FilePickerFileType>();
                            foreach (var filter in saveFileParams.Filters)
                            {
                                filters.Add(new FilePickerFileType(filter.Name ?? "All Files")
                                {
                                    Patterns = filter.Extensions ?? new List<string>()
                                });
                            }
                            options.FileTypeChoices = filters;
                        }

                        if (!string.IsNullOrEmpty(saveFileParams?.SuggestedStartLocation))
                        {
                            options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(saveFileParams.SuggestedStartLocation));
                        }

                        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                        result = file?.TryGetLocalPath() ?? null;
                    }
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown command: {message.Command}");
        }
        return result;
    }

    private void OnConnectionStateChanged(string state)
    {
        Dispatcher.UIThread.Post(() => PipeState = state);
    }

    private async Task OnPostMessageReceived(SpiritusMessage message)
    {
        var wasempty = _messageQueue.IsEmpty;

        _messageQueue.Enqueue(message);

        if (wasempty)
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                while (_messageQueue.TryDequeue(out var msg))
                {
                    _ = await DoDrawCommand(msg);
                }
            });
        }
    }

    private async Task<SpiritusResponse> OnInvokeMessageReceived(SpiritusMessage message)
    {
        return await Dispatcher.UIThread.InvokeAsync<SpiritusResponse>(async () =>
        {
            //LastMessage = $"invoke: {message.Command}";

            var result = await DoDrawCommand(message);

            return new SpiritusResponse
            {
                Response = "ok",
                Payload = result
            };
        }, DispatcherPriority.ApplicationIdle);
    }
}
