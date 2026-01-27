using System;
using Avalonia.Controls;
using AvaloniaNamedPipe.ViewModels;

namespace AvaloniaNamedPipe.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.InitializeNamedPipe(this);
        }        
    }
}