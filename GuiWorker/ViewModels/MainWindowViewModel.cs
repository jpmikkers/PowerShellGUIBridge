using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GuiWorker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiWorker;

namespace GuiWorker.ViewModels;

public class MyCustomClass
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public record class SPLine(double x1,double y1,double x2,double y2);

public record class SPRectangle(double x, double y, double w, double h, double? radiusX, double? radiusY);

public record class SPBrush
{
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}

public record class SPFileFilter
{
    public string? Name { get; set; }
    public List<string>? Extensions { get; set; }
}

public record class SPOpenFileDialog
{
    public string? Title { get; set; }
    public string? SuggestedStartLocation { get; set; }
    public bool? AllowMultiple { get; set; }
    public List<SPFileFilter>? Filters { get; set; }
}

public record class SPSaveFileDialog
{
    public string? Title { get; set; }
    public string? SuggestedStartLocation { get; set; }
    public string? DefaultExtension { get; set; }

    public string? SuggestedFileName
    {
        get; set;
    }

    public string? SuggestedFileType
    {
        get; set;
    }

    public bool? ShowOverwritePrompt { get; set; }

    public List<SPFileFilter>? Filters { get; set; }
}

public partial class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static

    private IBrush _fillBrush = Brushes.LightBlue;
    private IBrush _strokeBrush = Brushes.LightBlue;
    private double _strokeThickness = 1.0;

    [ObservableProperty]
    private string _pipeState = "idle";

    [ObservableProperty]
    private string _info = "idle";

    [ObservableProperty]
    private string _lastMessage = "";

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _powerShellOutput = "";

    public string[]? Args { get; set; }

    private NamedPipeClientService? _pipeClient;
    private SpiritusNamedPipeClient? _spiritusClient;
    private MainWindow _mainWindow = default!;
    private readonly ConcurrentQueue<SpiritusMessage> _messageQueue = new();

    public MainWindowViewModel()
    {
    }

    public void InitializeNamedPipe(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        if (Args is not null && Args.Length > 0)
        {
            _pipeClient = new NamedPipeClientService(Args[0], TimeSpan.FromSeconds(10.0));
            _spiritusClient = new SpiritusNamedPipeClient(_pipeClient);

            _spiritusClient.StateChanged += OnPipeStateChangedFromBlah;
            _spiritusClient.PostMessageReceived += OnPostMessageReceived;
            _spiritusClient.InvokeMessageReceived += OnInvokeMessageReceived;

            _pipeClient.StartAsync(CancellationToken.None).Wait();
        }
    }

    private async Task<object?> DoDrawCommand(SpiritusMessage message)
    {
        object? result = null;

        switch (message.Command)
        {
            case "FillBrush":
                {
                    var spbrush = message.Payload.Deserialize<SPBrush>();
                    if (spbrush is not null)
                    {
                        _fillBrush = new ImmutableSolidColorBrush(new Color(spbrush.A, spbrush.R, spbrush.G, spbrush.B));
                    }
                }
                break;

            case "StrokeBrush":
                {
                    var spbrush = message.Payload.Deserialize<SPBrush>();
                    if (spbrush is not null)
                    {
                        _strokeBrush = new ImmutableSolidColorBrush(new Color(spbrush.A, spbrush.R, spbrush.G, spbrush.B));
                    }
                }
                break;

            case "StrokeThickness":
                {
                    var spthickness = message.Payload.Deserialize<double?>();
                    _strokeThickness = spthickness ?? 1.0;
                }
                break;

            case "Line":
                {
                    var spline = message.Payload.Deserialize<SPLine>();
                    if (spline is not null)
                    {
                        var line = new Line()
                        {
                            StartPoint = new Avalonia.Point(spline.x1, spline.y1),
                            EndPoint = new Avalonia.Point(spline.x2, spline.y2),
                            Fill = _fillBrush,
                            Stroke = _strokeBrush,
                            StrokeThickness = _strokeThickness
                        };

                        _mainWindow.MyCanvas.Children.Add(line);
                    }
                }
                break;

            case "Rectangle":
                {
                    var sprect = message.Payload.Deserialize<SPRectangle>();

                    if (sprect is not null)
                    {
                        var rect = new Rectangle()
                        {
                            Width = sprect.w,
                            Height = sprect.h,
                            RadiusX = sprect.radiusX ?? 0.0,
                            RadiusY = sprect.radiusY ?? 0.0,
                            Fill = _fillBrush,
                            Stroke = _strokeBrush,
                            StrokeThickness = _strokeThickness
                        };

                        Canvas.SetLeft(rect, sprect.x);
                        Canvas.SetTop(rect, sprect.y);

                        _mainWindow.MyCanvas.Children.Add(rect);
                    }
                }
                break;

            case "Clear":
                _mainWindow.MyCanvas.Children.Clear();
                break;

            case "Sync":
                break;

            case "Info":
                var info = message.Payload.Deserialize<string>();
                this.Info = info;
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
        }
        return result;
    }

    private void OnPipeStateChangedFromBlah(string state)
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
                int count = 0;
                while (_messageQueue.TryDequeue(out var msg))
                {
                    //LastMessage = $"post: {message.Command}";
                    _ = await DoDrawCommand(msg);
                    count++;
                }

                //System.Diagnostics.Trace.WriteLine($"Processed {count} queued messages.");
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
